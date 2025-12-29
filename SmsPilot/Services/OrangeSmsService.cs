using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SmsPilot.Services
{
    public class OrangeSmsService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<OrangeSmsService> _logger;

        public OrangeSmsService(HttpClient httpClient, IConfiguration configuration, ILogger<OrangeSmsService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        // Ici je m'authentifie auprès de l'API Orange pour obtenir un token d'accès
        private async Task<string> GetAccessTokenAsync()
        {
            var clientId = _configuration["OrangeApi:ClientId"];
            var clientSecret = _configuration["OrangeApi:ClientSecret"];

            // J'encode les identifiants en Base64 pour l'authentification "Basic"
            var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{clientId}:{clientSecret}"));

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.orange.com/oauth/v3/token");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "client_credentials" }
            });

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Erreur Token Orange: {response.StatusCode}");
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var tokenData = JsonSerializer.Deserialize<OrangeTokenResponse>(json);

            return tokenData?.AccessToken ?? string.Empty;
        }

        // Maintenant j'envoie le SMS via l'API Orange
        public async Task<(bool Success, string? ApiMessageId)> SendSmsAsync(string recipientPhone, string messageContent)
        {
            try
            {
                // D'abord je récupère le token d'authentification
                string token = await GetAccessTokenAsync();
                if (string.IsNullOrEmpty(token)) return (false, null);

                // Ensuite je configure la requête avec les numéros
                string rawSender = _configuration["OrangeApi:SenderAddress"] ?? "tel:+2250000";

                // Normalisation des numéros pour respecter le pattern tel:\+\d{2}\d*
                // L'utilisateur ne met pas forcément l'indicatif (ex: 0788...) -> on ajoute 225
                string CleanPhoneNumber(string phone)
                {
                    if (string.IsNullOrWhiteSpace(phone)) return string.Empty;

                    // Je nettoie le numéro en ne gardant que les chiffres
                    string digits = new string(phone.Where(char.IsDigit).ToArray());

                    // Si le numéro ne commence pas par 225, je l'ajoute (indicatif de la Côte d'Ivoire)
                    if (!digits.StartsWith("225"))
                    {
                        digits = "225" + digits;
                    }

                    return $"tel:+{digits}";
                }

                string senderAddressFn = CleanPhoneNumber(rawSender);
                string recipientAddressFn = CleanPhoneNumber(recipientPhone);

                // Je construis l'URL de la requête
                // IMPORTANT : L'API Orange veut que le senderAddress dans l'URL soit encodé
                // tel:+225... devient tel%3A%2B225...
                string encodedSenderAddress = Uri.EscapeDataString(senderAddressFn);
                var requestUrl = $"https://api.orange.com/smsmessaging/v1/outbound/{encodedSenderAddress}/requests";

                var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                string senderName = _configuration["OrangeApi:SenderName"]; // Peut être null si non configuré

                // Je prépare le corps du message au format attendu par Orange
                var payload = new
                {
                    outboundSMSMessageRequest = new
                    {
                        address = recipientAddressFn,
                        senderAddress = senderAddressFn,
                        senderName = !string.IsNullOrEmpty(senderName) ? senderName : null,
                        outboundSMSTextMessage = new
                        {
                            message = messageContent
                        }
                    }
                };

                string jsonPayload = JsonSerializer.Serialize(payload);
                _logger.LogInformation($"[OrangeSMS] Envoi SMS vers {recipientAddressFn}. Payload:\n{jsonPayload}");

                request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                // J'envoie la requête à l'API
                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"[OrangeSMS] Erreur Envoi SMS ({response.StatusCode}): {responseContent}");
                    return (false, null);
                }
                else
                {
                    _logger.LogInformation($"[OrangeSMS] Succès ({response.StatusCode}): {responseContent}");
                    // Je récupère l'ID du message depuis la réponse de l'API
                    // La réponse contient outboundSMSMessageRequest.resourceURL
                    // au format : .../requests/{ID}
                    string? msgId = null;
                    try
                    {
                        using (JsonDocument doc = JsonDocument.Parse(responseContent))
                        {
                            if (doc.RootElement.TryGetProperty("outboundSMSMessageRequest", out var rootObj) &&
                                rootObj.TryGetProperty("resourceURL", out var urlProp))
                            {
                                string url = urlProp.GetString();
                                if (!string.IsNullOrEmpty(url))
                                {
                                    msgId = url.Split('/').Last();
                                }
                            }
                        }
                    }
                    catch (Exception parseEx)
                    {
                        _logger.LogWarning($"Erreur parsing ID: {parseEx.Message}");
                    }

                    return (true, msgId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception Envoi SMS: {ex.Message}");
                return (false, null);
            }
        }
        // Je récupère le solde SMS disponible depuis l'API Orange
        public async Task<int> GetSmsBalanceAsync()
        {
            try
            {
                string token = await GetAccessTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("Impossible de récupérer le token pour le solde.");
                    return 0;
                }

                var request = new HttpRequestMessage(HttpMethod.Get, "https://api.orange.com/sms/admin/v1/contracts");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.SendAsync(request);

                var json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {

                    return 0;
                }

                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    var root = doc.RootElement;

                    // Fonction locale pour chercher récursivement dans le JSON
                    int FindAvailableUnits(JsonElement element, int depth)
                    {
                        if (depth > 20) return 0; // Je limite la profondeur pour éviter un StackOverflow

                        if (element.ValueKind == JsonValueKind.Object)
                        {
                            // Le JSON réel montre directement "availableUnits" dans l'objet de contrat
                            // Exemple : [{"offerName":"SMS_OCB", "availableUnits":79, ...}]
                            if (element.TryGetProperty("availableUnits", out var units))
                            {
                                return units.GetInt32();
                            }

                            // Sinon, je parcours toutes les propriétés
                            foreach (var property in element.EnumerateObject())
                            {
                                var result = FindAvailableUnits(property.Value, depth + 1);
                                if (result > 0) return result;
                            }
                        }
                        else if (element.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var item in element.EnumerateArray())
                            {
                                var result = FindAvailableUnits(item, depth + 1);
                                if (result > 0) return result;
                            }
                        }
                        return 0;
                    }

                    return FindAvailableUnits(root, 0);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception Solde Orange : {ex.Message}");
                return 0;
            }
        }
    }

    public class OrangeTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }
    }
}
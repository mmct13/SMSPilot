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

        // 1. Authentification : Obtenir le Jeton d'accès (Bearer Token)
        private async Task<string> GetAccessTokenAsync()
        {
            var clientId = _configuration["OrangeApi:ClientId"];
            var clientSecret = _configuration["OrangeApi:ClientSecret"];

            // Encodage Base64 des identifiants pour l'authentification "Basic"
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

        // 2. Envoi du SMS
        public async Task<bool> SendSmsAsync(string recipientPhone, string messageContent)
        {
            try
            {
                // A. On récupère le token
                string token = await GetAccessTokenAsync();
                if (string.IsNullOrEmpty(token)) return false;

                // B. Configuration de la requête
                // Note : En mode Sandbox (Test), tu ne peux envoyer que vers tes numéros déclarés.
                // L'URL peut varier selon ton pays (ex: /smsmessaging/v1/outbound/tel:+2250000/requests)

                string rawSender = _configuration["OrangeApi:SenderAddress"] ?? "tel:+2250000";

                // Normalisation pour être sûr du format : tel:+225...
                string cleanNumber = rawSender.Replace("tel:", "").Replace("+", "").Trim();
                string senderAddress = $"tel:+{cleanNumber}";
                // L'encodage URL est important pour le '+' dans l'URL (tel:+225...) -> tel%3A%2B225...
                // Mais souvent l'API Orange accepte tel:+... ou demande tel%3A%2B...
                // On va utiliser System.Net.WebUtility.UrlEncode pour être sûr si l'API le demande, 
                // mais attention, HttpClient l'encode parfois déjà.
                // Pour l'instant on concatène simplement, si ça échoue on encodera.
                // Orange doc: /outbound/{senderAddress}/requests

                // Hack: L'API Orange attend souvent que le senderAddress dans l'URL soit encodé, 
                // spécifiquement le ':' et '+' doivent être traités correctement. 
                // Pour éviter des soucis on construit l'URL proprement.
                var requestUrl = $"https://api.orange.com/smsmessaging/v1/outbound/{Uri.EscapeDataString(senderAddress)}/requests";

                var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                // C. Corps du message (Format Orange)
                var payload = new
                {
                    outboundSMSMessageRequest = new
                    {
                        address = "tel:" + recipientPhone,
                        senderAddress = senderAddress,
                        outboundSMSTextMessage = new
                        {
                            message = messageContent
                        }
                    }
                };

                string jsonPayload = JsonSerializer.Serialize(payload);
                request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                // D. Envoi
                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorReason = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Erreur Envoi SMS Orange: {response.StatusCode} - {errorReason}");
                }

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception Envoi SMS: {ex.Message}");
                return false;
            }
        }
        // 3. Récupérer le solde SMS
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

                    // Fonction locale pour rechercher récursivement
                    int FindAvailableUnits(JsonElement element, int depth)
                    {
                        if (depth > 20) return 0; // Limite pour éviter StackOverflow

                        if (element.ValueKind == JsonValueKind.Object)
                        {
                            // Le JSON réel montre directement "availableUnits" dans l'objet de contrat
                            // Ex: [{"offerName":"SMS_OCB", "availableUnits":79, ...}]
                            if (element.TryGetProperty("availableUnits", out var units))
                            {
                                return units.GetInt32();
                            }

                            // Sinon, on parcourt les propriétés
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
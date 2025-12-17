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

        public OrangeSmsService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
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

            if (!response.IsSuccessStatusCode) return null;

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
                // Ici on utilise une URL générique souvent utilisée par Orange.
                var requestUrl = "https://api.orange.com/smsmessaging/v1/outbound/tel:+22500000000/requests";

                var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                // C. Corps du message (Format Orange)
                var payload = new
                {
                    outboundSMSMessageRequest = new
                    {
                        address = "tel:" + recipientPhone,
                        senderAddress = "tel:+22500000000",
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

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur Orange : {ex.Message}");
                return false;
            }
        }
    }

    public class OrangeTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }
    }
}
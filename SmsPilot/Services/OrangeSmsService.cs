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

        // 1. OBTENIR LE TOKEN (Authentification)
        private async Task<string> GetAccessTokenAsync()
        {
            var clientId = _configuration["OrangeApi:ClientId"];
            var clientSecret = _configuration["OrangeApi:ClientSecret"];

            // Encodage des identifiants en Base64 pour l'en-tête Authorization Basic
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
                // En cas d'erreur, on retourne null ou on peut logger l'erreur
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var tokenData = JsonSerializer.Deserialize<OrangeTokenResponse>(json);

            return tokenData?.AccessToken ?? string.Empty;
        }

        // 2. ENVOYER LE SMS
        public async Task<bool> SendSmsAsync(string recipientPhone, string messageContent)
        {
            try
            {
                // A. On récupère le jeton (Bearer Token)
                string token = await GetAccessTokenAsync();

                if (string.IsNullOrEmpty(token)) return false;

                // B. On prépare l'URL d'envoi
                // Note: En prod, l'expéditeur (+2250000...) doit souvent correspondre à celui déclaré chez Orange
                var senderPhone = _configuration["OrangeApi:SenderPhone"] ?? "+2250700000000";
                // L'URL attend le format tel:+225... encodé si nécessaire, mais ici on concatène simplement
                var requestUrl = $"https://api.orange.com/smsmessaging/v1/outbound/tel:{senderPhone}/requests";

                var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                // C. On construit le JSON spécifique attendu par Orange
                var payload = new
                {
                    outboundSMSMessageRequest = new
                    {
                        address = "tel:" + recipientPhone,
                        senderAddress = "tel:" + senderPhone,
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
            catch (Exception)
            {
                return false;
            }
        }
    }

    // Classe utilitaire pour lire la réponse du Token
    public class OrangeTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }
}
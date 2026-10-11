namespace skolenza.application.DTOS.Token
{
    /// <summary>
    /// Token-only response for the opt-in refresh API. The legacy login response remains unchanged.
    /// </summary>
    public class RefreshTokenV2ResponseDTO
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime TokenExpiry { get; set; }
        public DateTime RefreshTokenExpiresAtUtc { get; set; }
    }
}

namespace UiTestAutomation.Tests.Entities
{
    internal class MsalAccessTokenEntity : MsalCredentialEntity
    {
        public string CachedAt { get; set; }
        public string ExpiresOn { get; set; }
        public string ExtendedExpiresOn { get; set; }
    }
}

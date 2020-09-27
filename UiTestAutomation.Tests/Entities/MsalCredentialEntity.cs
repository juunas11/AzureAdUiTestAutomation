namespace UiTestAutomation.Tests.Entities
{
    internal abstract class MsalCredentialEntity
    {
        public string HomeAccountId { get; set; }
        public string Environment { get; set; }
        public string CredentialType { get; set; }
        public string ClientId { get; set; }
        public string Secret { get; set; }
        public string Realm { get; set; }
        public string Target { get; set; }
    }
}

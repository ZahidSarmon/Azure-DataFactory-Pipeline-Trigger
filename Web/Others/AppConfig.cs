namespace ADF.Web.Others;

public class AzureAd
{
	public string TenantId { get; set; }
	public string ClientId { get; set; }
	public string ClientSecret { get; set; }
	public string Instance { get; set; }
	public string Domain { get; set; }
	public string FactoryName { get; set; }
	public string ResourceGroupName { get; set; }
	public string SubscriptionId { get; set; }
	public string ClientCredentials { get; set; }
	public string Resource { get; set; }
}

public class ADFConfigure
{
        public string TenantId { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string FactoryName { get; set; }
        public string ResourceGroupName { get; set; }
        public string SubscriptionId { get; set; }
        public string GrantType { get; set; }
        public string Resource { get; set; }
}

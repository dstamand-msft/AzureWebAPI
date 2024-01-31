namespace AzureWebAPI
{
    public class AzureOptions
    {
        /// <summary>
        /// Gets or sets the storage account connection string
        /// </summary>
        /// <remarks>
        /// This proper has priority over <see cref="StorageAccountEndPoint"/> if set
        /// </remarks>
        public string StorageAccountConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the Storage Account Connection String
        /// </summary>
        public string StorageAccountEndPoint { get; set; }
    }
}
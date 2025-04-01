namespace OLSOrca.Services.Interfaces
{
    public interface ICosmosService
    {
        Task<List<T>> ReadItemAsync<T>(string containerName, string id);
        Task<bool> UpsertItemAsync<T>(string containerName, T item, string id);
    }
}

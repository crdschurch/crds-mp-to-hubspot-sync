namespace Crossroads.Service.HubSpot.Sync.Core.Serialization
{
    public interface IJsonSerializer
    {
        string Serialize<T>(T inputToBeSerialized);

        T Deserialize<T>(string serializedInput, string selector = null);
    }

    //public interface ISerializer<TSerialized, TDeserialized>
    //{
    //    TSerialized Serialize(TDeserialized obj);

    //    TDeserialized Deserialize(TSerialized obj);
    //}
}
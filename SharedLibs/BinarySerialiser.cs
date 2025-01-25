using System.Runtime.Serialization.Formatters.Binary;

namespace SharedLibs
{
    public static class BinarySerialiser
    {
        public static byte[] SerialiseBinary(this object instance)
        {
            return SerialiseToByteArray(instance);
        }

        public static T DeserialiseBinary<T>(this byte[] instance)
        {
            return (T)DeserialiseFromByteArray(instance);
        }

        public static T SerialiseCloneBinary<T>(this T instance)
        {
            return instance.SerialiseBinary().DeserialiseBinary<T>();
        }

        public static byte[] SerialiseToByteArray(object ObjectToSerialise)
        {
            byte[] returnedData = null;

            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    bf.Serialize(ms, ObjectToSerialise);
                    returnedData = ms.ToArray();
                }
            }
            catch { }

            return returnedData;
        }

        public static object DeserialiseFromByteArray(byte[] TheObject)
        {
            object deserialisedData = null;

            try
            {
                using (MemoryStream ms = new MemoryStream(TheObject))
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    deserialisedData = bf.Deserialize(ms);
                }
            }
            catch { }

            return deserialisedData;
        }
    }
}

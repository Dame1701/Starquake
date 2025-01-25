namespace SharedLibs;

using MessagePack;
using MessagePack.Formatters;

public class MultiDimIntArrayFormatter : IMessagePackFormatter<int[,,]>
{
    public void Serialize(ref MessagePackWriter writer, int[,,] value, MessagePackSerializerOptions options)
    {
        var dimensions = new int[value.Rank];
        for (var i = 0; i < value.Rank; i++)
        {
            dimensions[i] = value.GetLength(i);
        }

        // Flatten the multidimensional array
        var flattenedArray = new int[value.Length];
        Buffer.BlockCopy(value, 0, flattenedArray, 0, Buffer.ByteLength(value));

        // Write dimensions and flattened array
        writer.WriteArrayHeader(2); // Two elements: dimensions and data
        writer.WriteArrayHeader(dimensions.Length);
        foreach (var dim in dimensions)
        {
            writer.Write(dim);
        }
        writer.WriteArrayHeader(flattenedArray.Length);
        foreach (var item in flattenedArray)
        {
            writer.Write(item);
        }
    }

    public int[,,] Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        reader.ReadArrayHeader(); // Read the header for the outer array

        // Read dimensions
        var dimensionsCount = reader.ReadArrayHeader();
        var dimensions = new int[dimensionsCount];
        for (var i = 0; i < dimensionsCount; i++)
        {
            dimensions[i] = reader.ReadInt32();
        }

        // Read flattened data
        var dataCount = reader.ReadArrayHeader();
        var flattenedArray = new int[dataCount];
        for (var i = 0; i < dataCount; i++)
        {
            flattenedArray[i] = reader.ReadInt32();
        }

        // Dynamically reconstruct the multidimensional array
        var result = (int[,,])Array.CreateInstance(typeof(int), dimensions);
        Buffer.BlockCopy(flattenedArray, 0, result, 0, Buffer.ByteLength(result));

        return result;
    }
}

// Register the formatter
/*
var resolver = MessagePack.Resolvers.CompositeResolver.Create(
    new IMessagePackFormatter[] { new MultiDimArrayFormatter() },
    new[] { MessagePack.Resolvers.StandardResolver.Instance }
);

var options = MessagePackSerializerOptions.Standard.WithResolver(resolver);

// Use the formatter
int[,,] multiDimArray = new int[2, 2, 2]
{
    { { 1, 2 }, { 3, 4 } },
    { { 5, 6 }, { 7, 8 } }
};

byte[] serializedData = MessagePackSerializer.Serialize(multiDimArray, options);
int[,,] deserializedArray = MessagePackSerializer.Deserialize<int[,,]>(serializedData, options);

Console.WriteLine("Deserialized array:");
for (int i = 0; i < 2; i++)
for (int j = 0; j < 2; j++)
for (int k = 0; k < 2; k++)
    Console.WriteLine($"[{i},{j},{k}] = {deserializedArray[i, j, k]}");
*/
using System.Reflection;
using System.Runtime.Serialization;

namespace SharedLibs
{

    public class StarquakeDeserializationBinder : SerializationBinder
    {
        public override Type BindToType(string assemblyName, string typeName)
        {
            Type typeToDeserialize = null;

            // For each assemblyName/typeName that you want to deserialize to
            // a different type, set typeToDeserialize to the desired type.
            String assemVer1 = Assembly.GetExecutingAssembly().FullName;

            if (assemblyName.Contains("StarquakeXNA"))
            {
                // To use a type from a different assembly version, 
                // change the version number.
                // To do this, uncomment the following line of code.
                // assemblyName = assemblyName.Replace("1.0.0.0", "2.0.0.0");

                // To use a different type from the same assembly, 
                // change the type name.
                assemblyName = assemVer1;
                typeName = typeName.Replace("StarquakeXNA", "SharedLibs");
            }

            if (typeName.Contains("StarquakeXNA"))
            {
                typeName = typeName.Replace("StarquakeXNA", "SharedLibs");
            }

            // The following line of code returns the type.
            typeToDeserialize = Type.GetType(String.Format("{0}, {1}",
                typeName, assemblyName));

            return typeToDeserialize;
        }
    }
}

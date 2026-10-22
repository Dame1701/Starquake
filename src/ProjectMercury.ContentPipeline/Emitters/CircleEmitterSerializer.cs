// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

namespace ProjectMercury.ContentPipeline.Emitters;

//[ContentTypeSerializer]
//public class CircleEmitterSerializer : AbstractEmitterSerializer<CircleEmitter>
//{
//    protected override void Serialize(IntermediateWriter output, CircleEmitter value, ContentSerializerAttribute format)
//    {
//        base.Serialize(output, value, format);

//        output.Xml.WriteElementString("Radius", value.Radius.ToString());
//        output.Xml.WriteElementString("Ring", value.Ring.ToString());
//        output.Xml.WriteElementString("Radiate", value.Radiate.ToString());
//    }

//    protected override CircleEmitter Deserialize(IntermediateReader input, ContentSerializerAttribute format, CircleEmitter existingInstance)
//    {
//        CircleEmitter value = existingInstance ?? new CircleEmitter();

//        base.Deserialize(input, format, value);

//        value.Radius = Single.Parse(input.Xml.ReadElementString("Radius"));
//        value.Ring = Boolean.Parse(input.Xml.ReadElementString("Ring"));
//        value.Radiate = Boolean.Parse(input.Xml.ReadElementString("Radiate"));

//        return value;
//    }
//}
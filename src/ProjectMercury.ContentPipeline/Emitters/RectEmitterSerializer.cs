// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

namespace ProjectMercury.ContentPipeline.Emitters;

//[ContentTypeSerializer]
//public class RectEmitterSerializer : AbstractEmitterSerializer<RectEmitter>
//{
//    protected override void Serialize(IntermediateWriter output, RectEmitter value, ContentSerializerAttribute format)
//    {
//        base.Serialize(output, value, format);

//        output.Xml.WriteElementString("Width", value.Width.ToString());
//        output.Xml.WriteElementString("Height", value.Height.ToString());
//        output.Xml.WriteElementString("Rotation", value.Rotation.ToString());
//        output.Xml.WriteElementString("Frame", value.Frame.ToString());
//    }

//    protected override RectEmitter Deserialize(IntermediateReader input, ContentSerializerAttribute format, RectEmitter existingInstance)
//    {
//        RectEmitter value = existingInstance ?? new RectEmitter();

//        base.Deserialize(input, format, value);

//        value.Width = Single.Parse(input.Xml.ReadElementString("Width"));
//        value.Height = Single.Parse(input.Xml.ReadElementString("Height"));
//        value.Rotation = Single.Parse(input.Xml.ReadElementString("Rotation"));
//        value.Frame = Boolean.Parse(input.Xml.ReadElementString("Frame"));

//        return value;
//    }
//}
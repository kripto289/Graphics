using System;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;

namespace UnityEditor.VFX.Block
{

    static class VariadicSizeUtility
    {
        public enum SizeMode
        {
            X = 0,
            XY = 1,
            XYZ = 2,
        }

        public static IEnumerable<VFXAttributeInfo> GetAttributes(SizeMode mode, VFXAttributeMode attrMode)
        {
            yield return new VFXAttributeInfo(VFXAttribute.SizeX, attrMode);

            if ((int)mode > 0)
                yield return new VFXAttributeInfo(VFXAttribute.SizeY, attrMode);

            if ((int)mode > 1)
                yield return new VFXAttributeInfo(VFXAttribute.SizeZ, attrMode);
        }
    }

    class AttributeVariantVariadicSize : IVariantProvider
    {
        public Dictionary<string, object[]> variants
        {
            get
            {
                return new Dictionary<string, object[]>
                {
                    { "SizeMode", new object[]{VariadicSizeUtility.SizeMode.X, VariadicSizeUtility.SizeMode.XY, VariadicSizeUtility.SizeMode.XYZ} },
                };
            }
        }
    }

    [VFXInfo(category = "Size", variantProvider = typeof(AttributeVariantVariadicSize))]
    class SetSizeVariadic : VFXBlock
    {
        
        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), Tooltip("How the size should be handled (X = Square Sprites/Uniform 3D, XY = Rectangle Sprites, XYZ = 3D Particles")]
        public VariadicSizeUtility.SizeMode SizeMode = VariadicSizeUtility.SizeMode.X;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), Tooltip("How the new computed size is composed with its previous value")]
        public AttributeCompositionMode Composition;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), Tooltip("Controls whether the value is set directly or computed from a random range")]
        public RandomMode Random = RandomMode.Off;

        public override string name { get { return string.Format("{0} Size {1} {2}", VFXBlockUtility.GetNameString(Composition), SizeMode, VFXBlockUtility.GetNameString(Random)); } }

        public override VFXContextType compatibleContexts { get { return VFXContextType.kInitAndUpdateAndOutput; } }

        public override VFXDataType compatibleData { get { return VFXDataType.kParticle; } }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Age, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Lifetime, VFXAttributeMode.Read);

                if (Random != RandomMode.Off)
                    yield return new VFXAttributeInfo(VFXAttribute.Seed, VFXAttributeMode.ReadWrite); 

                var attrMode = (Composition == AttributeCompositionMode.Overwrite) ? VFXAttributeMode.Write : VFXAttributeMode.ReadWrite;

                foreach (var info in VariadicSizeUtility.GetAttributes(SizeMode, attrMode))
                    yield return info;

            }
        }

        private IEnumerable<string> skipInputProperties
        {
            get
            {
                if (Random != RandomMode.Off)
                    yield return "Value";
                else
                {
                    yield return "Min";
                    yield return "Max";
                }

                if (Composition != AttributeCompositionMode.Blend)
                    yield return "Blend";
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var typeName = "InputPropertiesX";
                switch (SizeMode)
                {
                    case VariadicSizeUtility.SizeMode.X:
                        typeName = "InputPropertiesX"; break;
                    case VariadicSizeUtility.SizeMode.XY:
                        typeName = "InputPropertiesXY"; break;
                    case VariadicSizeUtility.SizeMode.XYZ:
                        typeName = "InputPropertiesXYZ"; break;
                }

                var props = PropertiesFromType(typeName);
                return props.Where(o => !skipInputProperties.Any(a => a == o.property.name));
            }
        }

        public class InputPropertiesX
        {
            public float Value = 0.1f;
            public float Min = 0.0f;
            public float Max = 0.1f;
            [Range(0.0f, 1.0f),Tooltip("Size Blending factor")]
            public float Blend = 0.5f;
        }

        public class InputPropertiesXY
        {
            public Vector2 Value = Vector2.one * 0.1f;
            public Vector2 Min = Vector2.one * 0.0f;
            public Vector2 Max = Vector2.one * 0.1f;
            [Tooltip("Size Blending factor")]
            public Vector2 Blend = new Vector2(0.5f,0.5f);
        }

        public class InputPropertiesXYZ
        {
            public Vector3 Value = Vector3.one * 0.1f;
            public Vector3 Min = Vector3.one * 0.0f;
            public Vector3 Max = Vector3.one * 0.1f;
            [Tooltip("Size Blending factor")]
            public Vector3 Blend = new Vector3(0.5f, 0.5f, 0.5f);
        }

        public static string GetRandomMacroString(RandomMode random, VFXAttribute attribute, params string[] parameters)
        {
            if(random == RandomMode.Off)
                return parameters[0];
            else
                return string.Format("lerp({0},{1},{2})", parameters);
        }

        private string GetVariadicSource(VFXAttribute attribute, string randomString, string subScript = "")
        {
            string source = "";

            if (Random == RandomMode.Off)
                source = GetRandomMacroString(Random, attribute, "Value" + subScript, randomString);
            else
                source = GetRandomMacroString(Random, attribute, "Min" + subScript, "Max" + subScript, randomString);

            if (Composition == AttributeCompositionMode.Blend)
                source = VFXBlockUtility.GetComposeString(Composition, attribute.name, source, "Blend" + subScript);
            else
                source = VFXBlockUtility.GetComposeString(Composition, attribute.name, source);

            source += "\n";
            return source;
        }

        public override string source
        {
            get
            {
                string outSource = "";
                string randomString = "";

                if(Random == RandomMode.Uniform)
                {
                    outSource += "float random = RAND;";
                    randomString = "random";
                }

                if(Random == RandomMode.PerComponent)
                    randomString = "RAND";

                if ((int)SizeMode == 0)
                    outSource += GetVariadicSource(VFXAttribute.SizeX, randomString);
                else
                    outSource += GetVariadicSource(VFXAttribute.SizeX, randomString, ".x");

                if ((int)SizeMode > 0)
                    outSource += GetVariadicSource(VFXAttribute.SizeY, randomString, ".y");

                if ((int)SizeMode > 1)
                outSource += GetVariadicSource(VFXAttribute.SizeZ, randomString, ".z");

                return outSource;
            }
        }
    }
}

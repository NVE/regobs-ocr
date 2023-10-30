using SnowProfileScanner.Models;
using System.Globalization;
using System.Xml;
using static SnowProfileScanner.Models.SnowProfile;

namespace SnowProfileScanner.Services.Caaml
{
    public static class CaamlService
    {
        public static string SnowProfileSchemaUrl = "http://caaml.org/Schemas/SnowProfileIACS/v6.0.3";
        public static string GMLSchema = "http://www.opengis.net/gml";
        public static string NVESchema = "http://www.nve.no/snowprofile/1.0";

        public static string ConvertToCaaml(SnowProfile snowProfile)
        {
            if (snowProfile == null) return "";

            var doc = new XmlDocument();
            XmlElement root = doc.CreateElement("caaml", "SnowProfile", SnowProfileSchemaUrl);
            doc.AppendChild(root);

            doc.DocumentElement?.SetAttribute("xmlns:gml", GMLSchema);
            doc.DocumentElement?.SetAttribute("xmlns:nve", NVESchema);
            doc.DocumentElement?.SetAttribute("xmlns:caaml", SnowProfileSchemaUrl);
            doc.DocumentElement?.SetAttribute("xmlns:xsi", "http://www.w3.org/2001/XMLSchema-instance");

            var snowProfileResultsOfTag = doc.CreateChild("snowProfileResultsOf", doc.DocumentElement);
            snowProfileResultsOfTag.Prefix = "caaml";

            var snowProfileMeasurements = doc
                    .CreateChild("SnowProfileMeasurements", snowProfileResultsOfTag)
                    .AddAttribute("dir", "top down");

            if (snowProfile.Layers?.Any() == true)
            {
                var stratProfileEl = doc.CreateChild("stratProfile", snowProfileMeasurements);
                CreateLayerElements(doc, stratProfileEl, snowProfile.Layers);
            }

            // Snow temp
            if (snowProfile.SnowTemp?.Any() == true)
            {
                var tempObsTag = doc.CreateChild("tempProfile", snowProfileMeasurements);
                CreateTempElements(doc, tempObsTag, snowProfile.SnowTemp);
            }

            return doc.OuterXml;
        }

        public static void CreateLayerElements(
            XmlDocument doc,
            XmlElement profileEl,
            IEnumerable<SnowProfile.Layer> layers
        ) {
            var stratMetadataElement = doc.CreateChild("stratMetaData", profileEl);
            double depthTop = 0;

            foreach (var layer in layers)
            {
                var layerEl = doc.CreateChild("Layer", profileEl);

                var metadataElement = doc.CreateChild("metaData", layerEl);

                CreateCustomData(doc, layer, metadataElement);

                var value = depthTop.ToString();
                depthTop += layer.Thickness ?? 0;
                doc.CreateChild("depthTop", layerEl)
                    .AddAttribute("uom", "cm")
                    .SetInnerText(value);

                value = (layer.Thickness ?? 0).ToString();
                doc.CreateChild("thickness", layerEl)
                    .AddAttribute("uom", "cm")
                    .SetInnerText(value);

                if (!string.IsNullOrEmpty(layer.Grain))
                {
                    doc.CreateChild("grainFormPrimary", layerEl)
                        .SetInnerText(layer.Grain);
                }

                if (!string.IsNullOrEmpty(layer.GrainSecondary))
                {
                    doc.CreateChild("grainFormSecondary", layerEl)
                        .SetInnerText(layer.GrainSecondary);
                }

                if (layer?.Size > 0)
                {
                    var grainSizeEl = doc.CreateChild("grainSize", layerEl)
                        .AddAttribute("uom", "mm");

                    var grainSizeCompnents = doc.CreateChild("Components", grainSizeEl);

                    doc
                        .CreateChild("avg", grainSizeCompnents)
                        .SetInnerText(layer.Size.ToString() ?? "");

                    if (layer?.SizeMax > 0)
                    {
                        doc
                            .CreateChild("avgMax", grainSizeCompnents)
                            .SetInnerText(layer.SizeMax.ToString() ?? "");
                    }
                }

                if (!string.IsNullOrEmpty(layer?.Hardness))
                {
                    doc.CreateChild("hardness", layerEl)
                        .AddAttribute("uom", "")
                        .SetInnerText(layer.Hardness?.GetHardness() ?? "");
                }

                if (!string.IsNullOrEmpty(layer?.LWC))
                {
                    doc.CreateChild("wetness", layerEl)
                        .AddAttribute("uom", "")
                        .SetInnerText(layer?.LWC ?? "");
                }

                profileEl.AppendChild(layerEl);
            }
        }

        public static void CreateTempElements(
            XmlDocument doc,
            XmlElement tempObsTag,
            IEnumerable<SnowTemperature> tempObses
        ) {
            var tempObsMetadata = doc.CreateChild("tempMetaData", tempObsTag);
            foreach (var tempObs in tempObses)
            {
                if (tempObs.Depth is null || tempObs.Temp is null) continue;
                var obsTag = doc.CreateChild("Obs", tempObsTag);
                doc
                    .CreateChild("depth", obsTag)
                    .AddAttribute("uom", "cm")
                    .SetInnerText(tempObs?.Depth?.ToString() ?? "");
                doc
                    .CreateChild("snowTemp", obsTag)
                    .AddAttribute("uom", "degC")
                    .SetInnerText(tempObs?.Temp?.ToString() ?? "");
            }

        }

        private static void CreateCustomData(XmlDocument doc, SnowProfile.Layer layer, XmlElement metadataEl)
        {

            var bottomHardness = layer.Hardness.GetBottomHardness();
            var hasHardnessBottom = !string.IsNullOrEmpty(bottomHardness);
            if (hasHardnessBottom)
            {
                var customDataElement = doc.CreateChild("customData", metadataEl);
                doc.CreateChild("hardnessBottom", customDataElement, CaamlDocumentNamespaces.NVE)
                    .AddAttribute("uom", "")
                    .SetInnerText(bottomHardness);
            }
        }

        public static string GetPrimaryGrainForm(this string s)
        {
            if (s.Contains("("))
            {
                return s.Split("(").First();
            }
            return s;
        }

        public static string? GetSecondaryGrainForm(this string s)
        {
            if (s.Contains("("))
            {
                return s.Split("(").Last().Split(")").First();
            }
            return null;
        }

        public static string GetHardness(this string s)
        {
            if (s.Contains("/"))
            {
                return s.Split("/").First();
            }
            return s;
        }

        public static string? GetBottomHardness(this string s)
        {
            if (s is not null && s.Contains("/"))
            {
                return s.Split("/").Last();
            }
            return null;
        }

        public static XmlElement AddAttribute(this XmlElement doc, string name, string value)
        {
            doc.SetAttribute(name, value);
            return doc;
        }

        public static XmlElement SetInnerText(this XmlElement doc, string value)
        {
            doc.InnerText = value;
            return doc;
        }

        public static XmlElement CreateChild(this XmlDocument doc, string name, XmlElement parent, CaamlDocumentNamespaces schema = CaamlDocumentNamespaces.Caaml)
        {
            string namespaceUri;
            string elementPrefix;
            if (schema == CaamlDocumentNamespaces.Caaml)
            {
                namespaceUri = CaamlService.SnowProfileSchemaUrl;
                elementPrefix = "caaml";
            }
            else if (schema == CaamlDocumentNamespaces.GML)
            {
                namespaceUri = CaamlService.GMLSchema;
                elementPrefix = "gml";
            }
            else
            {
                elementPrefix = "nve";
                namespaceUri = CaamlService.NVESchema;
            }

            var element = doc.CreateElement(elementPrefix, name, namespaceUri);
            parent.AppendChild(element);
            return element;
        }

        public enum CaamlDocumentNamespaces
        {
            Caaml,
            GML,
            NVE
        }
    }
}

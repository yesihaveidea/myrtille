using System.Xml;
using System.Xml.XPath;

namespace Myrtille.Helpers
{
    public static class XmlTools
    {
        public static XmlNode GetNode(
            XmlNode parentNode,
            string name)
        {
            XmlNode theNode = null;

            if ((parentNode != null) &&
                (parentNode.ChildNodes != null) &&
                (parentNode.ChildNodes.Count > 0))
            {
                foreach (XmlNode node in parentNode.ChildNodes)
                {
                    if (node.Name.ToUpper().Equals(name.ToUpper()))
                    {
                        theNode = node;
                        break;
                    }
                }
            }

            return theNode;
        }

        public static XmlNode GetNode(
            XPathNavigator navigator,
            string path)
        {
            XmlNode node = null;

            var iterator = navigator.Select(path);
            if (iterator.Count == 1)
            {
                iterator.MoveNext();
                node = ((IHasXmlNode)iterator.Current).GetNode();
            }

            return node;
        }

        public static string ReadConfigKey(
            XmlNode parentNode,
            string key)
        {
            if ((parentNode != null) &&
                (parentNode.ChildNodes != null) &&
                (parentNode.ChildNodes.Count > 0))
            {
                XmlNode theNode = null;

                foreach (XmlNode node in parentNode.ChildNodes)
                {
                    if ((node.Name.ToUpper().Equals("ADD")) &&
                        (node.Attributes != null) &&
                        (node.Attributes["key"] != null) &&
                        (node.Attributes["key"].Value.ToUpper().Equals(key.ToUpper())))
                    {
                        theNode = node;
                        break;
                    }
                }

                if ((theNode != null) &&
                    (theNode.Attributes != null) &&
                    (theNode.Attributes["value"] != null))
                {
                    var theNodeValue = theNode.Attributes["value"];
                    return theNodeValue.Value;
                }
            }

            return null;
        }

        public static void WriteConfigKey(
            XmlNode parentNode,
            string key,
            string value)
        {
            if ((parentNode != null) &&
                (parentNode.ChildNodes != null) &&
                (parentNode.ChildNodes.Count > 0))
            {
                XmlNode theNode = null;

                foreach (XmlNode node in parentNode.ChildNodes)
                {
                    if ((node.Name.ToUpper().Equals("ADD")) &&
                        (node.Attributes != null) &&
                        (node.Attributes["key"] != null) &&
                        (node.Attributes["key"].Value.ToUpper().Equals(key.ToUpper())))
                    {
                        theNode = node;
                        break;
                    }
                }

                if ((theNode != null) &&
                    (theNode.Attributes != null) &&
                    (theNode.Attributes["value"] != null))
                {
                    var theNodeValue = theNode.Attributes["value"];
                    theNodeValue.Value = value;
                }
            }
        }
    }
}
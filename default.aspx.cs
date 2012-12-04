using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Xml;
using System.Text.RegularExpressions;

namespace RSSFunnel
{
    public partial class _default : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            GetRss();
        }

        private void GetRss()
        {
            Response.Clear();
            Response.ContentType = "application/rss+xml";

            var xml = new XmlDocument();
            xml.Load(Server.MapPath("~/App_Data/data.xml"));
            XmlNode source = xml.DocumentElement.SelectSingleNode("//Rss");

            var url = source.Attributes["Url"].Value;
            var xmlRss = new XmlDocument();
            xmlRss.Load(url);

            var items = xmlRss.DocumentElement.SelectNodes("//item");
            var rules = source["Rules"].SelectNodes("//Rule");
            var matches = new List<string>();
            foreach (XmlNode rule in rules)
            {
                var action = rule.Attributes["Action"].Value.Trim();
                var field = rule.Attributes["Field"].Value.Trim();
                var op = rule.Attributes["Operator"].Value.Trim();
                var what = rule.Attributes["What"].Value.Trim();

                foreach (XmlNode item in items)
                {
                    var searchText = "";
                    if (field == "*") searchText = Regex.Replace(item.InnerXml.Replace("&gt;", ">").Replace("&lt;", "<"), "<[^>]*>", "");
                    else
                    {
                        foreach (var cField in field.Split(','))
                        {
                            if (item[cField.Trim()] != null)
                                searchText += item[cField.Trim()].InnerText;
                        }
                    }

                    var condition = false;
                    switch (op)
                    {
                        case "eq": // equals
                            condition = searchText == what;
                            break;

                        case "nq": // not-equals
                            condition = searchText != what;
                            break;

                        case "cn": // contains
                            condition = searchText.Contains(what);
                            break;

                        case "nc": // not-contains
                            condition = !searchText.Contains(what);
                            break;
                    }

                    if ((condition && action == "block") || (!condition && action == "allow"))
                        matches.Add(item["guid"].InnerText);
                }

                foreach (var match in matches)
                {
                    var xElms = xmlRss.DocumentElement.SelectNodes("//item/guid[. = '" + match + "']/..");
                    foreach (XmlNode elm in xElms)
                    {
                        elm.ParentNode.RemoveChild(elm);
                    }
                }
            }

            Response.Write(xmlRss.InnerXml);
            Response.End();
        }
    }
}
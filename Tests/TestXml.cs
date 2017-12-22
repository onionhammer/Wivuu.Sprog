using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static System.Char;

namespace Wivuu.Sprog
{
    public class Document
    {
        public Node Root;

        public ParserError Error;

        public override string ToString() => Root.ToString();
    }

    public class Item { }

    public class Content : Item
    {
        public string Text;

        public override string ToString() => Text;
    }

    public class Node : Item
    {
        public string Name;
        public IEnumerable<Item> Children;

        public override string ToString()
        {
            if (Children != null)
                return string.Format("<{0}>", Name) +
                    Children.Aggregate("", (s, c) => s + c) +
                    string.Format("</{0}>", Name);
            return string.Format("<{0}/>", Name);
        }
    }

    public static class SprogXmlParser
    {
        static Parser ParseIdentifier(this Parser input, out string identifier) =>
            input.Skip(IsWhiteSpace)
                 .Take(IsLetter, out char first)
                 .Take(IsLetterOrDigit, out string rest)
                 .Let(identifier = string.Concat(first, rest))
                 .Skip(IsWhiteSpace);

        static Parser ParseTag(this Parser input, out string name, out bool selfClosing) =>
            input.SkipOne('<')
                 .ParseIdentifier(out name)
                 .Assert(name?.Length > 0 ? null : $"XML Tag must be at least 1 character long")
                 .Peek(out char nextC)
                 .Let(selfClosing = nextC == '/')
                 .SkipOne('>')
                 .Skip(IsWhiteSpace);

        static Parser ParseEndTag(this Parser input, string name) =>
            input.Skip("</")
                 .ParseIdentifier(out var endName)
                 .Assert(endName == name ? null : $"Expected end tag </{name}>")
                 .SkipOne('>')
                 .Skip(IsWhiteSpace);

        static Parser ParseItems(this Parser input, out List<Item> items)
        {
            bool NextItem(out Item next)
            {
                if (input.StartsWith("</"))
                {
                    next = null;
                    return false;
                }
                else if (input.StartsWith('<'))
                {
                    input = input.ParseNode(out var n);
                    next  = n;
                    return true;
                }
                else
                {
                    input = input.Take(c => c != '<', out string content);
                    next  = new Content { Text = content };
                    return true;
                }
            }

            items = new List<Item>(capacity: 1);
            while (NextItem(out var next))
                items.Add(next);

            return input;
        }

        static Parser ParseNode(this Parser input, out Node n) =>
            input.ParseTag(out var id, out var selfClosing)
                 .Rest(out var rest)
                 .Let(selfClosing ? rest.Let(n = new Node { Name = id })
                                  : rest.ParseItems(out var children)
                                        .Let(n = new Node { Name = id, Children = children })
                                        .ParseEndTag(id));

        public static bool TryParse(string xml, out Document document)
        {
            try
            {
                new Parser(xml)
                    .Skip(IsWhiteSpace)
                    .ParseNode(out var n);
               
                document = new Document { Root = n };
                return true;
            }
            catch (ParserException e)
            {
                document = new Document
                { 
                    Error = new ParserError(e.Remaining, e.Assertion).CalculateLineAndCol(xml) 
                };

                return false;
            }
        }
    }

    [TestClass]
    public class TestXml
    {
        readonly string[] GoodXml = 
        {
            @"<ul>
                <li>Item 1</li>
                <li>
                    <ul>
                        <li>Item 2.1</li>
                        <li>Item 2.2</li>
                        <li>Item 2.3</li>
                    </ul>
                </li>
                <li>Item 3</li>
                <li>Item 4</li>
                <li>Item 5</li>
            </ul>"
        };

        readonly string[] BadXml = 
        {
            @"<ul>
                <li>Item 4</lli>
            </ul>",
            @"<ul>
                <li>Item 4
            </ul>",
            @"<ul>
                <>Item 4</>
            </ul>"
        };

        [TestMethod]
        public void TestGoodXml()
        {
            Document doc;
            foreach (var good in GoodXml)
                Assert.IsTrue(SprogXmlParser.TryParse(good, out doc));
        }

        [TestMethod]
        public void TestBadXml()
        {
            Document doc;
            foreach (var bad in BadXml)
                Assert.IsFalse(SprogXmlParser.TryParse(bad, out doc));
        }
    }
}
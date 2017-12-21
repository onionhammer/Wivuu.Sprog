﻿using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sprache;
using static System.String;
using static System.Char;
using static csparser.Parser;

namespace benchmarks
{
    public class Program
    {
        #region Simple

        [Benchmark]
        public void InternalSimple()
        {
            ReadOnlySpan<char> TakeIdentifier(string input, out string _id) =>
                input.AsSpan()
                     .Skip(IsWhiteSpace)
                     .TakeOne(IsLetter, out var first)
                     .Take(IsLetterOrDigit, out var rest)
                     .Skip(IsWhiteSpace)
                     .Let(_id = Concat(first, rest));

            TakeIdentifier(" abc123  ", out var id);
            Assert.AreEqual("abc123", id);
        }

        [Benchmark]
        public void SpracheSimple() 
        {
            var identifier =
                from leading in Sprache.Parse.WhiteSpace.Many()
                from first in Sprache.Parse.Letter.Once()
                from rest in Sprache.Parse.LetterOrDigit.Many()
                from trailing in Sprache.Parse.WhiteSpace.Many()
                select new string(first.Concat(rest).ToArray());

            var id = identifier.Parse(" abc123  ");

            Assert.AreEqual("abc123", id);
        }

        #endregion

        #region Xml
        
        const string SourceXml = @"
        <ul>
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
        </ul>
        ";

        [Benchmark]
        public void InternalXmlRaw() =>
            csparser.XmlParser.TryParse(SourceXml, out var _);
        
        [Benchmark]
        public void InternalXmlContext() =>
            csparser.XmlParserContext.TryParse(SourceXml, out var _);
        
        [Benchmark]
        public void SpracheXml() =>
            SpracheXmlParser.Document.Parse(SourceXml);

        #endregion

        static void Main(string[] args) =>
            BenchmarkRunner.Run<Program>();
    }
}

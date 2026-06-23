using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Xunit;

namespace JsonSchemaToCSharp.Tests
{
    public class JsonRefWalkerTests
    {
        [Fact]
        public void ProcessElement_EnqueuesRelativeJsonRef()
        {
            var temp = Path.Combine(Path.GetTempPath(), "jsontest_rel" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temp);
            try
            {
                var refFile = Path.Combine(temp, "ref.json");
                File.WriteAllText(refFile, "{ \"foo\": 1 }");

                var json = "{ \"$ref\": \"ref.json#/\" }";
                using var doc = JsonDocument.Parse(json);

                var processed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var q = new Queue<string>();

                JsonRefWalker.ProcessElement(doc.RootElement, temp, processed, q);

                Assert.Single(q);
                var enqueued = q.Dequeue();
                Assert.Equal(Path.GetFullPath(refFile), Path.GetFullPath(enqueued));
            }
            finally
            {
                Directory.Delete(temp, recursive: true);
            }
        }

        [Fact]
        public void ProcessElement_EnqueuesFileUriJsonRef()
        {
            var temp = Path.Combine(Path.GetTempPath(), "jsontest_uri" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temp);
            try
            {
                var refFile = Path.Combine(temp, "ref2.json");
                File.WriteAllText(refFile, "{ \"x\": 2 }");

                var fileUri = new Uri(refFile).AbsoluteUri; // file:///C:/...
                var json = $"{{ \"$ref\": \"{fileUri}#/$defs/Foo\" }}";
                using var doc = JsonDocument.Parse(json);

                var processed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var q = new Queue<string>();

                JsonRefWalker.ProcessElement(doc.RootElement, temp, processed, q);

                Assert.Single(q);
                var enqueued = q.Dequeue();
                Assert.Equal(Path.GetFullPath(refFile), Path.GetFullPath(enqueued));
            }
            finally
            {
                Directory.Delete(temp, recursive: true);
            }
        }

        [Fact]
        public void ProcessElement_IgnoresFragmentOnlyRef()
        {
            var json = "{ \"$ref\": \"#/definitions/X\" }";
            using var doc = JsonDocument.Parse(json);

            var processed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var q = new Queue<string>();

            JsonRefWalker.ProcessElement(doc.RootElement, Directory.GetCurrentDirectory(), processed, q);

            Assert.Empty(q);
        }

        [Fact]
        public void ProcessElement_FindsNestedRefs()
        {
            var temp = Path.Combine(Path.GetTempPath(), "jsontest_nested" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temp);
            try
            {
                var refFile = Path.Combine(temp, "nested.json");
                File.WriteAllText(refFile, "{ \"y\": 3 }");

                var json = $"{{ \"arr\": [ {{ \"obj\": {{ \"$ref\": \"nested.json#\" }} }} ] }}";
                using var doc = JsonDocument.Parse(json);

                var processed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var q = new Queue<string>();

                JsonRefWalker.ProcessElement(doc.RootElement, temp, processed, q);

                Assert.Single(q);
                Assert.Equal(Path.GetFullPath(refFile), Path.GetFullPath(q.Dequeue()));
            }
            finally
            {
                Directory.Delete(temp, recursive: true);
            }
        }
    }
}

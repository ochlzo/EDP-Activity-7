using System.Text;
using edp_gui_app;

namespace edp_gui_app.Tests;

[TestClass]
public sealed class MultipartUploadParserTests
{
    [TestMethod]
    public void Parse_PreservesUploadedBytesAndFileIndex()
    {
        const string boundary = "----test";
        var body = Encoding.UTF8.GetBytes(
            $"--{boundary}\r\n" +
            "Content-Disposition: form-data; name=\"file1\"; filename=\"id.pdf\"\r\n" +
            "Content-Type: application/pdf\r\n\r\n" +
            "abc\u0000def\r\n" +
            $"--{boundary}--\r\n");

        var uploads = MultipartUploadParser.Parse(body, boundary);

        Assert.HasCount(1, uploads);
        Assert.AreEqual(1, uploads[0].FileIndex);
        Assert.AreEqual("id.pdf", uploads[0].FileName);
        CollectionAssert.AreEqual(Encoding.UTF8.GetBytes("abc\u0000def"), uploads[0].Content);
    }
}

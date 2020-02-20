

namespace Maina.HTTP.Data
{
	

	public class EmbedFieldData
	{
		public string Name;
		public string Value;
		public bool Inline = false;
	}

	public class EmbedData
	{
		public string [] Tags;
		public string Title;
		public string Description;
		public string URL;
		public uint? Color;
		public string IconURL;
		public string Author;
		public string AuthorURL;
		public string AuthorIconURL;
		public EmbedFieldData [] Fields;
		public string Footer;
		public string FooterIcon;

	}
}

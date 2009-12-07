using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Xml;
using System.Xml.Serialization;

namespace Spica.Xml.Feed.Simple
{
	/// <summary>
	/// Utility
	/// </summary>
	public class Utility
	{
		public static DateTime ParseDateTime(String dateTimeString)
		{
			DateTime dateTime;
			if (!DateTime.TryParse(dateTimeString, out dateTime))
				dateTime = DateTime.Now;

			return dateTime;
		}

		public static Uri ParseUri(String uriString)
		{
			if (String.IsNullOrEmpty(uriString))
				return null;

			return new Uri(uriString);
		}

		public static TResult ValueOrDefault<T, TResult>(T arg, Func<T, TResult> func)
			where T : class
		{
			if (arg == null)
				return default(TResult);

			return func(arg);
		}
	}

	/// <summary>
	/// Feed Document Interface
	/// </summary>
	public interface IFeedDocument
	{
		Uri Link { get; }
		String Title { get; }
		String Description { get; }
		List<IFeedItem> Items { get; }
	}

	/// <summary>
	/// Feed Item Interface
	/// </summary>
	public interface IFeedItem
	{
		String Author { get; }
		Uri Link { get; }
		String Title { get; }
		String Description { get; }
		DateTime PublishDate { get; }
	}

	#region RSS1.0
	[XmlRoot("RDF", Namespace = "http://www.w3.org/1999/02/22-rdf-syntax-ns#")]
	public class RSS10Document : IFeedDocument
	{
		#region XmlSerializer
		private static Object _sync = new object();
		private static XmlSerializer _serializer = null;
		static RSS10Document()
		{
			lock (_sync)
			{
				if (_serializer == null)
					_serializer = new XmlSerializer(typeof(RSS10Document));
			}
		}
		public static XmlSerializer Serializer { get { return _serializer; } }
		#endregion

		[XmlElement("channel", Namespace = "http://purl.org/rss/1.0/")]
		public RSS10Channel _channel;

		[XmlElement("item", Namespace = "http://purl.org/rss/1.0/")]
		public List<RSS10Item> _items;

		#region IFeedDocument メンバ
		[XmlIgnore()]
		public Uri Link { get { return _channel.Link; } }

		[XmlIgnore()]
		public string Title { get { return _channel.Title; } }

		[XmlIgnore()]
		public string Description { get { return _channel.Description; } }

		[XmlIgnore()]
		public List<IFeedItem> Items { get { return new List<IFeedItem>(_items.ToArray()); } }
		#endregion
	}

	public class RSS10Channel
	{
		#region XmlElements
		[XmlElement("title")]
		public String _title;

		[XmlElement("link")]
		public String _link;

		[XmlElement("description")]
		public String _description;

		[XmlElement("date", Namespace = "http://purl.org/dc/elements/1.1/")]
		public String _date;

		[XmlElement("language", Namespace = "http://purl.org/dc/elements/1.1/")]
		public String _language;
		#endregion

		[XmlIgnore()]
		public String Title { get { return _title; } }

		[XmlIgnore()]
		public Uri Link { get { return Utility.ParseUri(_link); } }

		[XmlIgnore()]
		public String Description { get { return _description; } }

		[XmlIgnore()]
		public DateTime PublishDate { get { return Utility.ParseDateTime(_date); } }
	}

	public class RSS10Item : IFeedItem
	{
		[XmlElement("title")]
		public String _title;

		[XmlElement("link")]
		public String _link;

		[XmlElement("description")]
		public String _description;

		[XmlElement("creator", Namespace = "http://purl.org/dc/elements/1.1/")]
		public String _creator;

		[XmlElement("date", Namespace = "http://purl.org/dc/elements/1.1/")]
		public String _date;

		#region IFeedItem メンバ
		[XmlIgnore()]
		public string Author { get { return _creator; } }

		[XmlIgnore()]
		public Uri Link { get { return Utility.ParseUri(_link); } }

		[XmlIgnore()]
		public string Title { get { return _title; } }

		[XmlIgnore()]
		public string Description { get { return _description; } }

		[XmlIgnore()]
		public DateTime PublishDate { get { return Utility.ParseDateTime(_date); } }
		#endregion
	}
	#endregion

	#region RSS2.0
	[XmlRoot("rss")]
	public class RSS20Document : IFeedDocument
	{
		#region XmlElements
		[XmlElement("channel")]
		public RSS20Channel _channel;
		#endregion

		#region Interface
		[XmlIgnore()]
		public String Title { get { return _channel._title; } }

		[XmlIgnore()]
		public String Description { get { return _channel._description; } }

		[XmlIgnore()]
		public Uri Link { get { return _channel.Link; } }

		[XmlIgnore()]
		public List<IFeedItem> Items { get { return new List<IFeedItem>(_channel._items.ToArray()); } }
		#endregion

		#region XmlSerializer
		private static Object _sync = new object();
		private static XmlSerializer _serializer = null;
		static RSS20Document()
		{
			lock (_sync)
			{
				if (_serializer == null)
					_serializer = new XmlSerializer(typeof(RSS20Document));
			}
		}
		public static XmlSerializer Serializer { get { return _serializer; } }
		#endregion
	}

	public class RSS20Channel
	{
		#region XmlElements
		[XmlElement("title")]
		public String _title;

		[XmlElement("description")]
		public String _description;

		[XmlElement("link")]
		public String _link;

		[XmlElement("pubDate")]
		public String _pubDate;

		[XmlElement("item")]
		public List<RSS20Item> _items;
		#endregion

		[XmlIgnore()]
		public String Title { get { return _title; } }

		[XmlIgnore()]
		public String Description { get { return _description; } }

		[XmlIgnore()]
		public Uri Link { get { return Utility.ParseUri(_link); } }

		[XmlIgnore()]
		public DateTime PublishDate { get { return Utility.ParseDateTime(_pubDate); } }
	}

	public class RSS20Item : IFeedItem
	{
		#region XmlElements
		[XmlElement("author")]
		public String _author;

		[XmlElement("link")]
		public String _link;

		[XmlElement("pubDate")]
		public String _pubDate;

		[XmlElement("title")]
		public String _title;

		[XmlElement("description")]
		public String _description;
		#endregion

		#region Interface
		[XmlIgnore()]
		public String Author { get { return _author; } }

		[XmlIgnore()]
		public Uri Link { get { return Utility.ParseUri(_link); } }

		[XmlIgnore()]
		public String Title { get { return _title; } }

		[XmlIgnore()]
		public String Description { get { return _description; } }

		[XmlIgnore()]
		public DateTime PublishDate { get { return Utility.ParseDateTime(_pubDate); } }
		#endregion
	}
	#endregion

	#region Atom1.0
	[XmlRoot("feed", Namespace = "http://www.w3.org/2005/Atom")]
	public class Atom10Document : IFeedDocument
	{
		#region XmlElements
		[XmlElement("title")]
		public String _title;

		[XmlElement("subtitle")]
		public String _subtitle;

		[XmlElement("link")]
		public List<Atom10Link> _link;

		[XmlElement("entry")]
		public List<Atom10Item> _items;
		#endregion

		#region Interface
		[XmlIgnore()]
		public String Title { get { return _title; } }

		[XmlIgnore()]
		public String Description { get { return _subtitle; } }

		[XmlIgnore()]
		public Uri Link { get { return _link.Count > 0 ? _link[0].HRef : null; } }

		[XmlIgnore()]
		public List<IFeedItem> Items { get { return new List<IFeedItem>(_items.ToArray()); } }
		#endregion

		#region XmlSerializer
		private static Object _sync = new object();
		private static XmlSerializer _serializer = null;
		static Atom10Document()
		{
			lock (_sync)
			{
				if (_serializer == null)
					_serializer = new XmlSerializer(typeof(Atom10Document));
			}
		}
		public static XmlSerializer Serializer { get { return _serializer; } }
		#endregion
	}

	public class Atom10Item : IFeedItem
	{
		#region XmlElements
		[XmlElement("author")]
		public Atom10Person _author;

		[XmlElement("link")]
		public List<Atom10Link> _link;

		[XmlElement("title")]
		public String _title;

		[XmlElement("content")]
		public String _content;

		[XmlElement("published")]
		public String _published;
		#endregion

		#region Interface
		[XmlIgnore()]
		public String Author { get { return _author.Name; } }

		[XmlIgnore()]
		public Uri Link { get { return _link.Count > 0 ? _link[0].HRef : null; } }

		[XmlIgnore()]
		public String Title { get { return _title; } }

		[XmlIgnore()]
		public String Description { get { return _content; } }

		[XmlIgnore()]
		public DateTime PublishDate { get { return DateTime.Parse(_published); } }
		#endregion
	}

	public class Atom10Person
	{
		#region XmlElements
		[XmlElement("name")]
		public String _name;

		[XmlElement("url")]
		public String _uri;

		[XmlElement("email")]
		public String _email;
		#endregion

		[XmlIgnore()]
		public String Name { get { return _name; } }

		[XmlIgnore()]
		public Uri Uri { get { return Utility.ParseUri(_uri); } }

		[XmlIgnore()]
		public String Email { get { return _email; } }
	}

	public class Atom10Link
	{
		#region XmlElements
		[XmlAttribute("href")]
		public String _href;

		[XmlAttribute("rel")]
		public String _rel;

		[XmlAttribute("type")]
		public String _type;

		[XmlAttribute("hreflang")]
		public String _hreflang;

		[XmlAttribute("title")]
		public String _title;

		[XmlAttribute("length")]
		public String _length;
		#endregion

		[XmlIgnore()]
		public Uri HRef { get { return Utility.ParseUri(_href); } }

		[XmlIgnore()]
		public String Rel { get { return _rel; } }

		[XmlIgnore()]
		public String Type { get { return _type; } }

		[XmlIgnore()]
		public String HrefLang { get { return _hreflang; } }

		[XmlIgnore()]
		public String Title { get { return _title; } }

		[XmlIgnore()]
		public String Length { get { return _length; } }
	}
	#endregion

	#region Feed Document
	public static class FeedDocument
	{
		public static IFeedDocument Load(String url)
		{
			using (XmlReader reader = XmlReader.Create(url))
			{
				return Load(reader);
			}
		}

		public static IFeedDocument Load(XmlReader reader)
		{
			if (RSS20Document.Serializer.CanDeserialize(reader))
			{
				return RSS20Document.Serializer.Deserialize(reader) as RSS20Document;
			}
			else if (RSS10Document.Serializer.CanDeserialize(reader))
			{
				return RSS10Document.Serializer.Deserialize(reader) as RSS10Document;
			}
			else if (Atom10Document.Serializer.CanDeserialize(reader))
			{
				return Atom10Document.Serializer.Deserialize(reader) as Atom10Document;
			}

			throw new Exception("Unknown feed format.");
		}
	}
	#endregion
}

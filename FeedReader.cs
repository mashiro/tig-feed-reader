using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using Misuzilla.Net.Irc;
using Misuzilla.Applications.TwitterIrcGateway;
using Misuzilla.Applications.TwitterIrcGateway.AddIns;
using Misuzilla.Applications.TwitterIrcGateway.AddIns.Console;
using Misuzilla.Applications.TwitterIrcGateway.AddIns.TypableMap;
using Spica.Xml.Feed;
using Spica.Applications.TwitterIrcGateway.AddIns.FeedReader.Properties;

namespace Spica.Applications.TwitterIrcGateway.AddIns.FeedReader
{
	public static class FeedReaderUtility
	{
		public static String Encrypt(String s)
		{
			if (!String.IsNullOrEmpty(s))
			{
				byte[] src = Encoding.UTF8.GetBytes(s);
				byte[] key = Encoding.UTF8.GetBytes(Resources.SharedKey);
				s = Convert.ToBase64String(CryptUtility.Encrypt(Aes.Create(), src, key));
			}

			return s;
		}

		public static String Decrypt(String s)
		{
			if (!String.IsNullOrEmpty(s))
			{
				byte[] src = Convert.FromBase64String(s);
				byte[] key = Encoding.UTF8.GetBytes(Resources.SharedKey);
				s = Encoding.UTF8.GetString(CryptUtility.Decrypt(Aes.Create(), src, key));
			}

			return s;
		}
	}

	public class FeedReceiveEventArgs : EventArgs
	{
		public IFeedDocument Document { get; private set; }
		public IFeedItem Item { get; private set; }
		public FeedReceiveEventArgs(IFeedDocument doc, IFeedItem item)
		{
			Document = doc;
			Item = item;
		}
	}

	public class FeedReaderUrlConfiguration : IConfiguration, IDisposable
	{
		[Description("フィードの URL を指定します")]
		public String Url { get; set; }

		[Description("チェックする間隔を秒単位で指定します")]
		public Int32 Interval { get; set; }

		[Description("コンテンツの形式を指定します (書式指定可)")]
		public String ContentFormat { get; set; }

		[Description("コンテンツを送るユーザ名を指定します (書式指定可)")]
		public String SenderNick { get; set; }

		[Description("コンテンツが流れるチャンネル名を指定します")]
		public String ChannelName { get; set; }		

		[Description("フィードを有効化または無効化します")]
		public Boolean Enabled { get; set; }

		[Description("改行コードの除去を有効かまたは無効化します")]
		public Boolean EnableRemoveLineBreak { get; set; }

		[Description("HTML タグの除去を有効化または無効化します")]
		public Boolean EnableRemoveHtmlTag { get; set; }

		[Description("エラーを無視するかどうかを指定します。")]
		public Boolean IgnoreWatchError { get; set; }

		[Browsable(false)]
		public String Username { get; set; }

		[Browsable(false)]
		public String Password { get; set; }

		[Browsable(false)]
		public DateTime LastPublishDate { get; set; }

		public event EventHandler<ErrorEventArgs> ErrorHandled;
		public event EventHandler PublishDateUpdated;
		public event EventHandler<FeedReceiveEventArgs> FeedItemReceived;
		private Timer _timer = null;

		public FeedReaderUrlConfiguration()
		{
			Url = String.Empty;
			Interval = 60 * 60;
			ContentFormat = "#{title} #{link}";
			SenderNick = "FeedReader";
			ChannelName = "#FeedReader";			
			Enabled = true;
			EnableRemoveLineBreak = false;
			EnableRemoveHtmlTag = false;
			IgnoreWatchError = true;
			Username = String.Empty;
			Password = String.Empty;
			LastPublishDate = DateTime.MinValue;
		}

		#region Crawl
		public void UpdateCrawlState()
		{
			if (Enabled && !String.IsNullOrEmpty(Url))
				StartCrawl();
			else
				EndCrawl();
		}

		public void StartCrawl()
		{
			if (_timer == null)
				_timer = new Timer(OnTimerCallback, null, Timeout.Infinite, Timeout.Infinite);

			Int32 intervalMillSec = Interval * 1000;
			_timer.Change(intervalMillSec, intervalMillSec);
		}

		public void EndCrawl()
		{
			if (_timer != null)
			{
				_timer.Dispose();
				_timer = null;
			}
		}

		public void CrawlForce()
		{
			ThreadPool.QueueUserWorkItem((state) => { OnTimerCallback(state); });
		}

		private void OnTimerCallback(Object state)
		{
			try
			{
				NetworkCredential credential = null;
				String password = FeedReaderUtility.Decrypt(Password);				
				if (!String.IsNullOrEmpty(Username) && !String.IsNullOrEmpty(password))
					credential = new NetworkCredential(Username, password);

				IFeedDocument feed = FeedDocument.Load(Url, credential);

				var updates = feed.Items.Where(item => item.PublishDate > LastPublishDate).ToList();
				if (updates.Count > 0)
				{
					LastPublishDate = updates.Max(item => item.PublishDate);
					if (PublishDateUpdated != null)
						PublishDateUpdated(this, EventArgs.Empty);

					updates.Sort((a, b) => Comparer<DateTime>.Default.Compare(a.PublishDate, b.PublishDate));
					foreach (var item in updates)
					{
						if (FeedItemReceived != null)
							FeedItemReceived(this, new FeedReceiveEventArgs(feed, item));
					}
				}
			}
			catch (Exception ex)
			{
				if (ErrorHandled != null)
					ErrorHandled(this, new ErrorEventArgs(ex));
			}
		}
		#endregion

		public void Dispose()
		{
			EndCrawl();
		}

		public override string ToString()
		{
			return ToShortString();
		}

		public string ToShortString()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendFormat("{0} ({1})", Url, Interval);
			return sb.ToString();
		}

		public string ToLongString()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendFormat("[{0}]", Enabled ? "*" : " ");
			sb.AppendFormat(" {0} ({1})", Url, Interval);
			return sb.ToString();
		}
	}

	public class FeedReaderConfiguration : IConfiguration, IDisposable
	{
		[Description("TypableMapを有効化または無効化します")]
        public Boolean EnableTypableMap { get; set; }

		[Browsable(false)]
		public List<FeedReaderUrlConfiguration> Items { get; set; }

		public FeedReaderConfiguration()
		{
			EnableTypableMap = false;
			Items = new List<FeedReaderUrlConfiguration>();
		}

		[Browsable(false)]
		public void Dispose()
		{
			foreach (var item in Items)
			{
				item.Dispose();
			}
		}

		[Browsable(false)]
		public void Setup(FeedReaderAddIn addIn)
		{
			foreach (var item in Items)
			{
				addIn.RegisterEvent(item);
				item.UpdateCrawlState();
			}
		}
	}

	[Description("フィードの設定を行うコンテキストに切り替えます")]
	public class FeedReaderContext : Context
	{
		private FeedReaderAddIn AddIn { get { return CurrentSession.AddInManager.GetAddIn<FeedReaderAddIn>(); } }

		public override IConfiguration[] Configurations { get { return new IConfiguration[] { AddIn.Config }; } }
		protected override void OnConfigurationChanged(IConfiguration config, System.Reflection.MemberInfo memberInfo, object value)
		{
			if (config is FeedReaderConfiguration)
			{
				AddIn.Config = config as FeedReaderConfiguration;
				AddIn.SaveConfig();
			}
		}

		[Description("指定したフィードを強制的に更新します")]
		public void UpdateForce(String arg)
		{
			FindAt(arg, item =>
			{
				item.CrawlForce();
				Console.NotifyMessage(String.Format("フィード {0} を更新しました。", item));
			});
		}

		[Description("存在するフィードをすべて表示します")]
		public void List()
		{
			if (AddIn.Config.Items.Count == 0)
			{
				Console.NotifyMessage("フィードは現在設定されていません。");
				return;
			}

			for (Int32 i = 0; i < AddIn.Config.Items.Count; ++i)
			{
				var item = AddIn.Config.Items[i];
				Console.NotifyMessage(String.Format("{0}: {1}", i, item.ToLongString()));
			}
		}

		[Description("指定したフィードを有効化します")]
		public void Enable(String arg)
		{
			SwitchEnable(arg, true);
		}

		[Description("指定したフィードを無効化します")]
		public void Disable(String arg)
		{
			SwitchEnable(arg, false);
		}

		[Description("指定したフィードを削除します")]
		public void Remove(String arg)
		{
			FindAt(arg, item =>
			{
				item.Dispose();
				AddIn.Config.Items.Remove(item);
				AddIn.SaveConfig();
				Console.NotifyMessage(String.Format("フィード {0} を削除しました。", item));
			});
		}

		[Description("指定したフィードを編集します")]
		public void Edit(String arg)
		{
			FindAt(arg, item =>
			{
				var context = Console.GetContext<FeedReaderEditContext>(CurrentServer, CurrentSession) as FeedReaderEditContext;
				context.Item = item;
				context.IsNew = false;
				Console.PushContext(context);
			});
		}

		[Description("フィードを新規追加します")]
		public void New()
		{
			FeedReaderEditContext context = Console.GetContext<FeedReaderEditContext>(CurrentServer, CurrentSession) as FeedReaderEditContext;

			var item = new FeedReaderUrlConfiguration();
			AddIn.RegisterEvent(item);

			context.Item = item;
			context.IsNew = true;
			Console.PushContext(context);
		}

		private void SwitchEnable(String arg, Boolean enable)
		{
			FindAt(arg, item =>
			{
				item.Enabled = enable;
				AddIn.SaveConfig();
				Console.NotifyMessage(String.Format("フィード {0} を{1}化しました。", item, (enable ? "有効" : "無効")));

				item.UpdateCrawlState();
			});
		}

		private void FindAt(String arg, Action<FeedReaderUrlConfiguration> action)
		{
			Int32 index;
			if (Int32.TryParse(arg, out index))
			{
				if (index < AddIn.Config.Items.Count && index > -1)
				{
					action(AddIn.Config.Items[index]);
				}
				else
				{
					Console.NotifyMessage("存在しないフィードが指定されました。");
				}
			}
			else
			{
				Console.NotifyMessage("設定のフィードが正しくありません。");
			}
		}
	}

	public class FeedReaderEditContext : Context
	{
		private FeedReaderAddIn AddIn { get { return CurrentSession.AddInManager.GetAddIn<FeedReaderAddIn>(); } }
		public FeedReaderUrlConfiguration Item { get; set; }
		public Boolean IsNew { get; set; }
		private Boolean _urlChanged = false;

		public override IConfiguration[] Configurations { get { return new IConfiguration[] { Item }; } }
		public override string ContextName { get { return (IsNew ? "New" : "Edit") + typeof(FeedReaderUrlConfiguration).Name; } }

		protected override void OnConfigurationChanged(IConfiguration config, System.Reflection.MemberInfo memberInfo, object value)
		{
			if (config is FeedReaderUrlConfiguration)
			{
				if (memberInfo.Name == "Url")
				{
					_urlChanged = true;
				}
			}
		}

		[Description("フィードの取得を試みます")]
		public void Test()
		{
			Item.LastPublishDate = DateTime.MinValue;
			Item.CrawlForce();
			Console.NotifyMessage("フィードの取得を試みます");
		}

		[Description("書式指定子の一覧を表示します")]
		public void ShowFormat()
		{
			Console.NotifyMessage(Resources.FormatMessage);
		}

		[Description("BASIC 認証に使用するユーザ名を設定します")]
		public void Username(String s)
		{
			if (!String.IsNullOrEmpty(s))
				Item.Username = s;
			Console.NotifyMessage(String.Format("Username = {0}", Item.Username));
		}

		[Description("BASIC 認証に使用するパスワードを設定します")]
		public void Password(String s)
		{
			if (!String.IsNullOrEmpty(s))
				Item.Password = FeedReaderUtility.Encrypt(s);
			Console.NotifyMessage(String.Format("Password = {0}", FeedReaderUtility.Decrypt(Item.Password)));
		}

		[Description("フィードを保存してコンテキストを終了します")]
		public void Save()
		{
			// 状態を保存
			if (_urlChanged) Item.LastPublishDate = DateTime.MinValue;
			if (IsNew) AddIn.Config.Items.Add(Item);
			AddIn.SaveConfig();

			if (!CurrentSession.Groups.ContainsKey(Item.ChannelName))
			{
				// グループを作成する
				Misuzilla.Applications.TwitterIrcGateway.Group group = new Misuzilla.Applications.TwitterIrcGateway.Group(Item.ChannelName);
				CurrentSession.Groups.Add(Item.ChannelName, group);
				CurrentSession.JoinChannel(CurrentSession, group);
				Console.NotifyMessage(String.Format("グループ名 {0} を作成しました。", Item.ChannelName));
				CurrentSession.SaveGroups();
			}

			// クローラの状態を更新
			Item.UpdateCrawlState();

			Console.NotifyMessage(String.Format("フィードを{0}しました。", (IsNew ? "新規作成" : "保存")));
			Exit();
		}
	}

	public class FeedReaderAddIn : AddInBase
	{
		public FeedReaderConfiguration Config { get; set; }
		private Regex _regexLineBreak = new Regex(@"\r\n|\r|\n");
		private Regex _regexHtmlTag = new Regex(@"<[^>]*>");
		private DateTime _lastSaveTime;

		public FeedReaderAddIn()
		{
			Config = null;
			_lastSaveTime = DateTime.Now;
		}

		public override void Initialize()
		{
			base.Initialize();

			Config = CurrentSession.AddInManager.GetConfig<FeedReaderConfiguration>();
			Config.Setup(this);

			CurrentSession.AddInsLoadCompleted += (sender, e) =>
			{
				CurrentSession.AddInManager.GetAddIn<ConsoleAddIn>().RegisterContext<FeedReaderContext>();
			};
		}

		public override void Uninitialize()
		{
			SaveConfig();
			Config.Dispose();

			base.Uninitialize();
		}

		internal void SaveConfig()
		{
			CurrentSession.AddInManager.SaveConfig(Config);
		}

		internal void OnErrorHandled(object sender, ErrorEventArgs e)
		{
			var item = sender as FeedReaderUrlConfiguration;
			if (!item.IgnoreWatchError)
			{
				CurrentSession.SendChannelMessage(item.ChannelName, item.SenderNick, e.Exception.Message, true, false, false, true);
#if DEBUG
				CurrentSession.SendChannelMessage(item.ChannelName, item.SenderNick, e.Exception.StackTrace, true, false, false, true);
#endif
			}
		}

		internal void OnPublishDateUpdated(object sender, EventArgs e)
		{
			// 毎回保存するとたぶん爆発する
			//SaveConfig();

			// 前回の保存から1時間経過していたら保存する
			DateTime now = DateTime.Now;
			TimeSpan span = now - _lastSaveTime;
			if (span.TotalHours >= 1)
			{
				SaveConfig();
				_lastSaveTime = now;
			}
		}

		internal void OnFeedItemReceived(object sender, FeedReceiveEventArgs e)
		{
			var config = sender as FeedReaderUrlConfiguration;

			String replacedSender = ReplaceFormatedString(config.SenderNick, config, e.Document, e.Item);
			String replacedContent = ReplaceFormatedString(config.ContentFormat, config, e.Document, e.Item);
			replacedContent = AppendTypableMap(replacedContent, FeedItemToStatus(e.Item));

			foreach (String line in replacedContent.Split('\n'))
			{
				PrivMsgMessage priv = new PrivMsgMessage();
				priv.Sender = replacedSender;
				priv.Receiver = config.ChannelName;
				priv.Content = line;
				CurrentSession.Send(priv);
			}
		}

		internal void RegisterEvent(FeedReaderUrlConfiguration item)
		{
			item.ErrorHandled += new EventHandler<ErrorEventArgs>(OnErrorHandled);
			item.PublishDateUpdated += new EventHandler(OnPublishDateUpdated);
			item.FeedItemReceived += new EventHandler<FeedReceiveEventArgs>(OnFeedItemReceived);			
		}

		private String ReplaceFormatedString(String str, FeedReaderUrlConfiguration config, IFeedDocument doc, IFeedItem item)
		{
			Func<String, String> conv = s =>
			{
				if (String.IsNullOrEmpty(s))
					return String.Empty;

				// HTML デコード
				s = Utility.UnescapeCharReference(s);

				if (config.EnableRemoveLineBreak)
				{
					// 改行コードを削除
					s = _regexLineBreak.Replace(s, String.Empty);
				}
				else
				{
					// 改行コードを LF(\n) に統一
					s = _regexLineBreak.Replace(s, "\n");
				}

				// HTMLタグを削除
				if (config.EnableRemoveHtmlTag)
				{
					s = _regexHtmlTag.Replace(s, String.Empty);
				}

				return s;
			};

			StringBuilder sb = new StringBuilder(str);
			sb.Replace("#{feed_title}", conv(doc.Title));
			sb.Replace("#{feed_link}", conv(doc.Link.ToString()));
			sb.Replace("#{feed_description}", conv(doc.Description));
			sb.Replace("#{author}", conv(item.Author));
			sb.Replace("#{link}", conv(item.Link.ToString()));
			sb.Replace("#{title}", conv(item.Title));
			sb.Replace("#{description}", conv(item.Description));
			sb.Replace("#{publish_date}", conv(item.PublishDate.ToString()));

			return sb.ToString();
		}

		/// <summary>
		/// FeedItem から Status に無理矢理変換
		/// </summary>
		private Status FeedItemToStatus(IFeedItem item)
		{
			return new Status()
			{
				CreatedAt = item.PublishDate,
				Source = item.Link.ToString(),
				Text = item.Description,
				User = new User()
				{
					Name = item.Title,
					ScreenName = item.Author,
				},
			};
		}

		/// <summary>
		/// TypableMapの情報を付与
		/// </summary>
		private String AppendTypableMap(String str, Status status)
		{
			if (Config.EnableTypableMap)
			{
				var typableMapCommands = CurrentSession.AddInManager.GetAddIn<TypableMapSupport>().TypableMapCommands;
				if (typableMapCommands != null)
				{
					String typableMapId = typableMapCommands.TypableMap.Add(status);

					// TypableMapKeyColorNumber = -1 の場合には色がつかなくなる
					if (CurrentSession.Config.TypableMapKeyColorNumber < 0)
						return str + String.Format(" ({0})", typableMapId);
					else
						return str + String.Format(" \x0003{0}({1})", CurrentSession.Config.TypableMapKeyColorNumber, typableMapId);
				}
			}

			return str;
		}
	}
}

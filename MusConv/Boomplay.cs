using System.Web;
using HtmlAgilityPack;
using RestSharp;

namespace Test
{
	public class BoomplayClient
	{
		private readonly RestClient _client = new RestClient();
		private string _cookie = "isSupportWebP=T; imei=5PudqzDQEhzlUYsPCG; valid=T; phoneCountryCode=380; phone=qebkNGPIbGKBkLuvli+leg==; countryCode=UA; _gid=GA1.2.2122488629.1659968494; birthday=; country=; iconMagicUrl=; accountType=byPhone; sex=M; sign=; vipType=; avatar=; tmpPw=39b3f21b553704c4854a4ab3da9e1b65; userName=Lexamania; afid=144612358; isVip=F; name=Lexamania; sessionID=N_shYBrdt1IitBSUQbOqtvaEeu3KPLAABjms5wuBSx8vI-; _gat_gtag_UA_184500473_2=1; _ga=GA1.2.1592589208.1659393767; JSESSIONID=81E5253133070C5E3AB0215675EB2E29; _ga_TYGC13V7V3=GS1.1.1660243127.20.1.1660246436.0";

		public async Task GetPlaylistTracks()
		{
			var playlistId = 51952077;
			var content = await callApi(
				BoomplayConstants.ROOT_URL + $"playlists/{playlistId}",
				Method.GET,
				null,
				getAddHeaders(BoomplayConstants.ROOT_URL + $"playlists/{playlistId}")
			);
			if (content == null) return ;

			var document = new HtmlDocument();
			document.LoadHtml(content);

			var musicsNode = document.DocumentNode.SelectSingleNode("//ol[@class='noneSelect morePart_musics']");
			var musicNodes = musicsNode?.ChildNodes.Where(x => x.Name == "li").ToList();
			var musicsData = musicNodes?.Select(x => x.Attributes.FirstOrDefault(a => a.Name == "data-data")).ToList();
			var result = musicsData?.Where(x => x != null)
				.Select(x => {
					var data = DecodeHtmlText(x.Value).Split("@ #");
					return new MusicShort()
					{
						Id = data[0],
						PlaylistId = data[2],
						Image = data[3],
						Name = data[4],
						Artist = data[5],
						ArtistId = data[6],
						Duration = data[7],
					};
				}).ToList();
		}

		
		#region Private methods
		private string DecodeHtmlText(string text)
		{
			string result = HttpUtility.UrlDecode(text);
			if (result != text) return DecodeHtmlText(result);

			return result;
		}

		private void setHeaders(RestRequest request, Dictionary<string, string> headers = null)
		{
			request.AddHeader("user-agent", BoomplayConstants.AGENT);
			request.AddHeader("accept-encoding", "gzip, deflate, br");
			request.AddHeader("content-type", "application/json;charset=UTF-8");
			request.AddHeader("accept-language", "en-US,en;q=0.9,ru;q=0.8");
			request.AddHeader("x-requested-with", "XMLHttpRequest");
			request.AddHeader("coockie", _cookie);

			if (headers != null)
			{
				foreach (var header in headers)
					request.AddHeader(header.Key, header.Value);
			}
		}

		private Dictionary<string, string> getAddHeaders(string referer = null)
		{
			var addHeader = new Dictionary<string, string>();
			if (referer != null) addHeader.Add("referer", referer);
			return addHeader;
		}

		private async Task<string> callApi(string url, Method method, object obj = null, Dictionary<string, string> headers = null)
		{
			var request = new RestRequest(url, method);
			setHeaders(request, headers);

			if (obj != null) request.AddJsonBody(obj);

			var response = await _client.ExecuteAsync(request, method);
			return response.IsSuccessful ? response.Content : null;
		}

		#endregion
	}

	public static class BoomplayConstants
	{
		public static string ROOT_URL = "https://www.boomplay.com/";
		public static string SOURCE_URL = "https://source.boomplaymusic.com/";
		public static string AGENT = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/104.0.0.0 Safari/537.36";

		public static string PrettyUrl(string imageId)
		{
			return SOURCE_URL + imageId;
		}
	}

	public class MusicShort
	{
		public string Id { get; set; }
		public string PlaylistId { get; set; }
		public string ArtistId { get; set; }
		public string Artist { get; set; }
		public string Name { get; set; }
		public string Image { get; set; }
		public string Duration { get; set; }
	}
}
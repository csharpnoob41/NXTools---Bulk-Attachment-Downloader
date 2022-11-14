// See https://aka.ms/new-console-template for more information
//using Newtonsoft.Json;
using System.Text.Json;
using System.Web;
using System.Net;
using RestSharp;
using System.Diagnostics;
using System.Reflection;

internal class Program
{
    private static async Task Main(string[] args)
    {
        globalValues.currentPathPrefix = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        string configPath = @"\config.json";
        string loadConfig = File.ReadAllText(globalValues.currentPathPrefix+configPath);
        config currentConfig = new config();
        currentConfig = JsonSerializer.Deserialize<config>(loadConfig);


        

        globalValues.Client_Secret = currentConfig.Client_Secret;
        globalValues.Client_ID = currentConfig.Client_ID;
        globalValues.Bbkey = currentConfig.BbKey;

        Console.WriteLine($"Your Client secret is: {globalValues.Client_Secret}");
        Console.WriteLine($"Your Client ID is: {globalValues.Client_ID}");
        Console.WriteLine($"Your Dev Key is: {globalValues.Bbkey}");

        await GetInitialAuthCode();
        Thread.Sleep(2000);
        await GetConstituents();
        await RequestConstituentAttachments();

        Console.WriteLine("Press Enter to Continue");
        Console.ReadLine();


        static async Task GetConstituents()
        {
            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true

            };
            string constituentPath = @$"{globalValues.currentPathPrefix}\constituents.json";
            string uri = "https://api.sky.blackbaud.com/constituent/v1/constituents";
            string _nextLink = string.Empty;
            int count = new int();
            List<constituent> _constituentIDs = new List<constituent>();
            constituentResponse _constituentResponse = new constituentResponse();
            RestClient client = new RestClient()
                   .AddDefaultHeader("Authorization", $"Bearer {globalValues.Access_token}")
                   .AddDefaultHeader("Bb-Api-Subscription-Key", globalValues.Bbkey)
                   ;

            //Make a single request to get the count 
            RestRequest _request = new RestRequest($"{uri}?fields=id,last,first&limit=5000", Method.Get);

            var _response = await client.ExecuteGetAsync(_request);


            _constituentResponse = JsonSerializer.Deserialize<constituentResponse>(_response.Content);

            if(File.Exists(constituentPath) && (new FileInfo(constituentPath).Length > 6))
            {
                _constituentIDs = JsonSerializer.Deserialize<List<constituent>>(constituentPath);
                if(_constituentIDs.Count == _constituentResponse.count)
                {
                    return;
                }
                else
                {
                    Console.WriteLine("Application Constituent count doesn't match NXT \n Rebuilding...");
                }

            }
            while (_constituentIDs.Count < _constituentResponse.count)
            {
                _response = await client.ExecuteGetAsync(_request);
                globalValues.currentCalls++;
                _constituentResponse = JsonSerializer.Deserialize<constituentResponse>(_response.Content);
                foreach(var cons in _constituentResponse.value)
                {
                    _constituentIDs.Add(cons);
                }
                _request = new RestRequest($"{_constituentResponse.next_link}", Method.Get);

                _constituentResponse = JsonSerializer.Deserialize<constituentResponse>(_response.Content);


            }
            await File.WriteAllTextAsync(constituentPath, JsonSerializer.Serialize<List<constituent>>(_constituentIDs, jsonOptions));




        }

        static async Task RequestConstituentAttachments()
        {
            string jsonLoad;
            string constituentPath = @$"{globalValues.currentPathPrefix}\constituentList.json";
            string progressPath = @$"{globalValues.currentPathPrefix}\progress.json";
            int status = 1;

            jsonLoad = File.ReadAllText(constituentPath);

            List<constituent> tempConstLoad = new List<constituent>();

            List<constituent> constituentIDs = new List<constituent>();

            List<attachment> attachmentList = new List<attachment>();

            List<string> constituentProgress = new List<string>();

            if (File.Exists(progressPath) && (new FileInfo(progressPath).Length >= 6))
            {
                Console.WriteLine("Loading previous attachment entries...");
                HashSet<constituent> _constituentHash = JsonSerializer.Deserialize<HashSet<constituent>>(await File.ReadAllTextAsync(constituentPath));
                List<string> _progressCheck = JsonSerializer.Deserialize<List<string>>(await File.ReadAllTextAsync(progressPath)); 

                Console.WriteLine("Parsing for previously scanned constituents...");
                Console.WriteLine("Now removing previously scanned constituents...");

                var compareSet = new HashSet<constituent>();

                foreach(var item in _constituentHash)
                {
                    if (!(_progressCheck.Contains(item.id)))
                    {
                        constituentIDs.Add(item);
                    }
                    else
                    {
                        continue;
                    }
                }    

                Console.WriteLine($"There are now {constituentIDs.Count()} left to parse");
                Thread.Sleep(3000);
            }
            else
            {
                Console.WriteLine("Loading constituents...");
                Thread.Sleep(3000);
                await File.WriteAllTextAsync(progressPath,null);
                constituentIDs = JsonSerializer.Deserialize<List<constituent>>(File.ReadAllText(constituentPath));
               
            }



            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);


            string uri;
            client.DefaultRequestHeaders.Add("Bb-Api-Subscription-Key", $"{globalValues.Bbkey}");
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {globalValues.Access_token}");



            foreach (var obj in constituentIDs)
            
            {
                uri = "https://api.sky.blackbaud.com/constituent/v1/constituents/"+ obj.id.ToString() + "/attachments";
                
                var response = await client.GetAsync(uri);
                globalValues.currentCalls++;

                if ((int)response.StatusCode == 401)
                {
                    while (!response.IsSuccessStatusCode)
                    {

                        Console.WriteLine("Getting new Auth Token");
                        await RefreshToken();
                        client.DefaultRequestHeaders.Remove("Authorization");
                        client.DefaultRequestHeaders.Remove("refresh_token");
                        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {globalValues.Access_token}");
                        client.DefaultRequestHeaders.Add("refresh_token", $"Bearer {globalValues.Refresh_token}");

                        response = await client.GetAsync(uri);
                       
                    }
                }

                
                //Main body of the program. This is where attachments are scanned and loaded
                else if ((int)response.StatusCode == 200)
                {

                    attachmentResponse _attachmentResponse = new attachmentResponse();
                    attachment _attachmentLoad = new attachment();
                    //Gets attachment response
                    string res = await response.Content.ReadAsStringAsync();
                    int responseInt = (int)response.StatusCode;
                    globalValues.currentCalls++;

                    _attachmentResponse = JsonSerializer.Deserialize<attachmentResponse>(res);
                    List<attachment> _currConAttachList = new List<attachment>();
                    foreach(var att in _attachmentResponse.value)
                    {
                        _currConAttachList.Add(att);
                    }
                 
                    Console.WriteLine($"Working on ID Num {obj.id}...{status}/{constituentIDs.Count} Status Code: {response.StatusCode.ToString()} with status code {responseInt}");
                    var jsonOptions = new JsonSerializerOptions
                    {
                        WriteIndented = true
                        
                    };

                    attachmentResponse atRes = JsonSerializer.Deserialize<attachmentResponse>(res);
                    List<attachment> atListFromFile;
                    string downDir;
                    int track = 1;
                    using (WebClient webClient = new WebClient())
                    {
                        //webClient.DownloadFileCompleted += (s, e) => Console.WriteLine("Download complete");
                        //webClient.DownloadProgressChanged += (s, e) => Console.WriteLine($"Downloading... {e.ProgressPercentage}%");

                        foreach(var att in _currConAttachList)
                        {
                            downDir = $@"{globalValues.currentPathPrefix}\Blackbaud Downloads\Constituent Downloads\{obj.first}, {obj.last} - {obj.id}\";

                            if (!Directory.Exists(downDir))
                            {
                                Directory.CreateDirectory(downDir);
                            }
                            Console.WriteLine($"Now downloading attachment {track}/{_currConAttachList.Count}");
                            await webClient.DownloadFileTaskAsync(new Uri(att.url),downDir+@$"\{obj.last} {obj.first} @{obj.id}@ - {att.file_name}");
                            track++;
                        }
                        webClient.Dispose();

                    }


                    //Loads constituent progress, writes new constituent ID to file.
                    List<string> constLoad;

                    if (File.Exists(progressPath) && (new FileInfo(progressPath).Length >= 6))
                    {
                        constLoad = JsonSerializer.Deserialize<List<string>>(await File.ReadAllTextAsync(progressPath));
                    }
                    else
                    {
                        constLoad = new List<string>();
                    }
                    constLoad.Add(obj.id);
                    await File.WriteAllTextAsync(progressPath, JsonSerializer.Serialize<List<string>>(constLoad));
                    status++;
                }
                else
                {
                    Console.WriteLine("Something went wrong");
                    Console.ReadLine();
                }

                if(globalValues.currentCalls >= globalValues.maxCalls)
                {
                    Console.WriteLine("You have reached your maximum number of API calls/24hr period. Run the application tomorrow, it will pick back up where you left off.");
                    break;
                }

            }
            
            Console.WriteLine($"Scanned constituents for attachments.");
            Console.WriteLine("Press Enter to continue");
            Console.ReadLine();



        }

        static async Task RefreshToken()
        {
            string jsonResponse;
            token newToken = new token();

            //Make post request to fetch new token
            var client = new RestClient("https://oauth2.sky.blackbaud.com");
            var request = new RestRequest("/token",Method.Post)
                .AddHeader("content-type", "application/x-www-form-urlencoded")
                .AddParameter("grant_type", "refresh_token")
                .AddParameter("refresh_token",globalValues.Refresh_token)
                .AddParameter("client_id", globalValues.Client_ID)
                .AddParameter("client_secret", globalValues.Client_Secret)
                ;

            var response = await client.PostAsync<token>(request);

            Console.WriteLine("New token granted");
            Console.WriteLine(response.access_token);

            globalValues.Access_token = response.access_token;
            globalValues.Refresh_token = response.refresh_token;

        }

        static async Task GetInitialAuthCode()
        {
            Console.WriteLine("Getting initial authorization code information.");
            HttpListener listener = new HttpListener();

            string client_id = globalValues.Client_ID;
            string uri = "http://localhost:5000";

            //Spawns a weblistener to retrieve the response from Blackbaud's Auth endpoint
            listener.Prefixes.Add("http://localhost:5000/");
            listener.Start();
            Process.Start(new ProcessStartInfo($@"https://app.blackbaud.com/oauth/authorize?client_id={globalValues.Client_ID}&response_type=code&redirect_uri={uri}") { UseShellExecute = true });





            HttpListenerContext context = listener.GetContext();
            HttpListenerRequest request = context.Request;

            var queryString = HttpUtility.ParseQueryString(request.RawUrl);

            var code = queryString["/?code"];   
            
            //Closes the listener
            listener.Stop();

            //Make a post request to get initial Auth tokens
            var client = new RestClient("https://oauth2.sky.blackbaud.com");
            var tokenRequest = new RestRequest("/token", Method.Post)
                .AddParameter("grant_type", "authorization_code")
                .AddParameter("code", code.ToString())
                .AddParameter("redirect_uri", uri)
                .AddParameter("client_id",globalValues.Client_ID)
                .AddParameter("client_secret",globalValues.Client_Secret)
                .AddHeader("content_type", "application/x-www-form-urlencoded")
                ;

            var tokenResponse = await client.PostAsync<token>(tokenRequest);

            globalValues.Access_token = tokenResponse.access_token;
            globalValues.Refresh_token = tokenResponse.refresh_token;


        }

    }
}

public class constituent
{
    public string id { get; set; }
    public string first { get; set; }

    public string last { get; set; } 

    public List<attachment> attachments { get; set; }   
}
public class attachment
{
    public string url { get; set; }
    public string id { get; set; }
    public string name { get; set; }
    public string date { get; set; }
    public string parent_id { get; set; }
    public List<string> tags { get; set; }
    public string type { get; set; }
    
    public string file_name { get; set; }

    public string first_name { get; set; }

    public string last_name { get; set; }

}

static class globalValues
{
    public static string Bbkey { get; set; }

    public static string Access_token { get; set; }
    public static string Refresh_token { get; set; }

    public static string Client_ID { get; set; }
    public static string Client_Secret { get; set; }

    public static string currentPathPrefix { get; set; }

    public static int maxCalls = 24999;

    public static int currentCalls = 0;
}

public class token
{
    public  string access_token { get; set; }
    public  string token_type { get; set; }
    public  string refresh_token { get; set; }
    public  string environment_id { get; set; }
    public  string environment_name { get; set; }
    public  string legal_entity_id { get; set; }
    public  string legal_entity_name { get; set; }
    public  string user_id { get; set; }

    public  string email { get; set; }
    public  string family_name { get; set; }
    public  string given_name { get; set; }

}

public class constituentResponse
{
    public int count { get; set; }
    public string next_link { get; set; }
    public List<constituent> value { get; set; }
}

public class config
{
    public string BbKey { get; set; }
    public string Client_ID { get; set; }
    public string Client_Secret { get; set; }
}

public class attachmentResponse
{
    public List<attachment> value { get; set; }
}
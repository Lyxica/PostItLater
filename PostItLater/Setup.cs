﻿namespace PostItLater
{
    using System;
    using System.Net;
    using Newtonsoft.Json;
    using RestSharp;
    using RestSharp.Authenticators;

    /// <summary>
    /// This class is responsible for registering the application into the reddit users account.
    /// </summary>
    public class Setup
    {
        private readonly string landingUrl = "http://localhost:7926/";
        private readonly string clientId;
        private readonly Guid guid = Guid.NewGuid();

        /// <summary>
        /// Initializes a new instance of the <see cref="Setup"/> class.
        /// </summary>
        /// <param name="clientId">Client ID of the registered reddit application.</param>
        public Setup(string clientId)
        {
            this.clientId = clientId;
        }

        /// <summary>
        /// Authenticate program with user's reddit profile.
        /// </summary>
        /// <returns><see cref="APIKey"/>.</returns>
        public APIKey Run()
        {
            System.Diagnostics.Process.Start(string.Format(
                                                    "https://www.reddit.com/api/v1/authorize?client_id={0}&" +
                                                    "response_type=code&" +
                                                    "state={2}&" +
                                                    "redirect_uri={1}&" +
                                                    "duration=permanent&" +
                                                    "scope=submit",
                                                    this.clientId,
                                                    this.landingUrl,
                                                    this.guid.ToString()));
            var code = this.GetCode();
            var token = this.GetToken(code);
            return new APIKey(token.access_token, token.refresh_token, token.expires_in);
        }

        private string GetCode()
        {
            var httpclient = new HttpListener();
            httpclient.Prefixes.Add(this.landingUrl);
            httpclient.Start();
            var context = httpclient.GetContext();
            var query = context.Request.QueryString;
            if (query["error"] != null) { Console.Error.WriteLine("Error"); }
            if (query["state"] != this.guid.ToString()) { Console.Error.WriteLine("Error"); }
            context.Response.Redirect("https://www.reddit.com/prefs/apps/");
            context.Response.Close();
            httpclient.Stop();
            return query["code"];
        }

        private Token GetToken(string code)
        {
            var client = new RestClient("https://www.reddit.com/");
            client.Authenticator = new HttpBasicAuthenticator(this.clientId, string.Empty);
            var request = new RestRequest("api/v1/access_token", Method.POST);
            request.AddParameter("grant_type", "authorization_code");
            request.AddParameter("code", code);
            request.AddParameter("redirect_uri", this.landingUrl);
            var result = client.Execute(request);
            Console.WriteLine(result.Content);

            var token = JsonConvert.DeserializeObject<Token>(result.Content);
            return token;
        }
    }
}

using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Splintermate.Models;

namespace Splintermate.Delegation.Models
{
    public class Account : IAccount
    {
        public Account(IConfiguration configuration)
        {
            ValidateConfigurationAndSetInstance(configuration);
        }

        public string Username { get; set; }
        public string PostingKey { get; set; }
        public string ActiveKey { get; set; }

        private void ValidateConfigurationAndSetInstance(IConfiguration configuration)
        {
            var hasSection = configuration.GetSection("Delegation").GetChildren().Any(c => c.Key.Equals("Account"));

            if (!hasSection)
                throw new NullReferenceException("Section [Delegation:Account] is not defined in the delegation.json");

            var username = configuration.GetValue<string>("Delegation:Account:Username");

            if (string.IsNullOrEmpty(username))
                throw new NullReferenceException("Value [Delegation:Account.Username] is not defined in the delegation.json");

            Username = username;

            var postingKey = configuration.GetValue<string>("Delegation:Account:PostingKey");

            if (string.IsNullOrEmpty(postingKey))
                throw new NullReferenceException("Section [Delegation:Account.PostingKey] is not defined in the delegation.json");

            PostingKey = postingKey;

            var activeKey = configuration.GetValue<string>("Delegation:Account:ActiveKey");

            ActiveKey = activeKey;
        }
    }
}
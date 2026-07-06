using Frostvein.Core;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using WindowsFirewallHelper;
using WindowsFirewallHelper.Addresses;

namespace Frostvein.GameObject.Networking
{
    public class AntiSpamModule : Singleton<AntiSpamModule>
    {
        private bool _isBusy;

        private ConcurrentBag<string> _ipAddresses { get; } = new ();

        public void RunBlacklistTask()
        {
            Observable.Interval(TimeSpan.FromSeconds(30)).Subscribe(x =>
            {
                if (_ipAddresses.Count < 10)
                {
                    return;
                }

                _isBusy = true;

                var copy = new ConcurrentBag<string>(_ipAddresses);
                _ipAddresses.Clear();

                var ruleName = $"blacklist: {copy.FirstOrDefault()}";

                var rule = FirewallManager.Instance.CreatePortRule(FirewallProfiles.Public, ruleName,
                    FirewallAction.Block, 3000, FirewallProtocol.TCP);

                rule.LocalPorts = new ushort[] { 3000, 3001, 3002, 3003, 3004, 3005, 3006, 3007, 3008, 3009, 3010, 3011, 3012, 3013, 5678, 5100 };

                var ruleContent = rule.RemoteAddresses.ToList();
                ruleContent.Clear();
                rule.RemoteAddresses = ruleContent.ToArray();

                foreach (var ip in copy)
                {
                    var success = IPAddress.TryParse(ip, out var addressBytes);

                    if (!success)
                    {
                        continue;
                    }

                    ruleContent.Add(new SingleIP(addressBytes.GetAddressBytes()));
                }

                rule.RemoteAddresses = ruleContent.ToArray();
                FirewallManager.Instance.Rules.Add(rule);
                copy.Clear();
                _isBusy = false;
            });
        }

        public void AddToList(string ipAddress)
        {
            if (_isBusy)
            {
                return;
            }

            if (string.IsNullOrEmpty(ipAddress))
            {
                return;
            }

            if (_ipAddresses.Contains(ipAddress))
            {
                return;
            }

            _ipAddresses.Add(ipAddress);
        }
    }
}

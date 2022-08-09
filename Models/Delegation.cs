using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Splintermate.Delegation.Models
{
	public class Delegation
	{
		public string[] Players { get; set; }
		public DelegationCard[] Cards { get; set; }
		public DelegationTokens Tokens { get; set; }
	}

	public class DelegationCard
	{
		public int Id { get; set; }
		public int? Bcx { get; set; }
		public bool Gold { get; set; }
	}

	public class DelegationTokens
	{
		public string Token { get; set; }
		public int Threshold { get; set; }
		public int Quantity { get; set; }
	}
}

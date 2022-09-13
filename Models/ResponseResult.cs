using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Splintermate.Delegation.Models
{
	public class ResponseResult<T>
	{
		[JsonProperty("overallResult")]
		public bool OverallResult { get; set; }

		[JsonProperty("result")]
		public T Result { get; set; }

		[JsonProperty("messages")]
		public string[] Messages { get; set; }

		public ResponseResult(bool overallResult, T result, string[] messages = null)
		{
			OverallResult = overallResult;
			Result = result;
			Messages = messages;
		}
	}
}

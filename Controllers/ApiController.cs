using Microsoft.AspNetCore.Mvc;
using Splintermate.Delegation.Models;
using Splintermate.Delegation.Sinks;
using ILogger = Serilog.ILogger;

namespace Splintermate.Delegation.Controllers;

public class ApiController : Controller
{
    private readonly CardDelegationService _cardDelegationService;
    private readonly TokenDelegationService _tokenDelegationService;
    private readonly ILogger _logger;
    private readonly Account _account;

    public ApiController(CardDelegationService cardDelegationService, TokenDelegationService tokenDelegationService, IConfiguration configuration, ILogger logger)
    {
        _cardDelegationService = cardDelegationService;
        _tokenDelegationService = tokenDelegationService;
        _logger = logger;
        _account = new Account(configuration);
    }

    [HttpGet("/api/account")]
    public IActionResult Account()
    {
        return new JsonResult(new ResponseResult<string>(true, _account.Username));
    }

    [HttpPost("/api/cards/delegate")]
    public IActionResult DelegateCards([FromBody] CardDelegationBody body)
    {
        if (_cardDelegationService.IsRunning)
        {
            return new JsonResult(new ResponseResult<object>(false, null, new[] { "delegation-active" }));
        }

        MemorySink.Instance.LogEvents.Clear();

        _ = _cardDelegationService.Run(body.Cards, body.Players);

        return new JsonResult(new ResponseResult<object>(true, null));
    }

    [HttpPost("/api/tokens/transfer")]
    public IActionResult TransferTokens([FromBody] TokenTransferBody body)
    {
        if (_tokenDelegationService.IsRunning)
        {
            return new JsonResult(new ResponseResult<object>(false, null, new[] { "transfer-active" }));
        }

        MemorySink.Instance.LogEvents.Clear();

        _ = _tokenDelegationService.Run(body.Players, body.Mode, body.Quantity, body.Threshold, body.Token);

        return new JsonResult(new ResponseResult<object>(true, null));
    }

    [HttpGet("/api/cancel")]
    public IActionResult CancelAction()
    {
        if (!_cardDelegationService.IsRunning && !_tokenDelegationService.IsRunning)
        {
            return new JsonResult(new ResponseResult<object>(true, null, new[] { "nothing-active" }));
        }

        _cardDelegationService.Cancel();
        _tokenDelegationService.Cancel();

        return new JsonResult(new ResponseResult<object>(true, null));
    }

    [HttpGet("/api/state")]
    public IActionResult Logs()
    {
        var messages = MemorySink
            .Instance
            .LogEvents
            .Where(x => x.Properties.Any(p => p.Key == "Type" && p.Value.ToString() == "\"Delegation\""))
            .Select(x => new
                {
                    Message = x.RenderMessage(),
                    Level = x.Level
                }
            )
            .ToArray();

        return new JsonResult(new ResponseResult<object>(true, new
        {
            Active = _cardDelegationService.IsRunning || _tokenDelegationService.IsRunning,
            Logs = messages
        }));
    }
}
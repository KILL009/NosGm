using MediatR;
using Microsoft.AspNetCore.Mvc;
using NosGm.GameObject.Modules.Bazaar.Commands;
using NosGm.GameObject.Modules.Bazaar.Queries;
using System.Threading.Tasks;

namespace NosTale.Module.Bazaar.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class BazaarController : ControllerBase
    {
        private readonly IMediator _mediator;

        public BazaarController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("GetState/{id}")]
        public async Task<IActionResult> GetState(long id)
        {
            return Ok(await _mediator.Send(new GetStateQuery { Id = id }));
        }

        [HttpGet("DeleteItem/{id}")]
        public async Task<IActionResult> DeleteBazaarItem(long id)
        {
            return Ok(await _mediator.Send(new DeleteBazaarItemCommand { Id = id }));
        }

        [HttpPost("SetState")]
        public async Task<IActionResult> SetState([FromBody] SetStateCommand command)
        {
            return Ok(await _mediator.Send(command));
        }

        [HttpDelete("DeleteState/{id}")]
        public async Task<IActionResult> DeleteState(long id)
        {
            return Ok(await _mediator.Send(new DeleteStateCommand { Id = id }));
        }

        [HttpGet("GetItem/{id}")]
        public async Task<IActionResult> GetBazaarItem(long id)
        {
            return Ok(await _mediator.Send(new GetBazaarItemQuery { Id = id }));
        }

        [HttpPost("InsertOrUpdate")]
        public async Task<IActionResult> InsertOrUpdateBazaarItem([FromBody] InsertOrUpdateBazaarItemCommand command)
        {
            return Ok(await _mediator.Send(command));
        }

        [HttpPost("CommitListing")]
        public async Task<IActionResult> CommitBazaarListing([FromBody] CommitBazaarListingCommand command)
        {
            return Ok(await _mediator.Send(command));
        }

        [HttpPost("Rcs")]
        public async Task<IActionResult> GenerateRcsList([FromBody] GetRcsListQuery command)
        {
            return Ok(await _mediator.Send(command));
        }

        [HttpPost("Rcb")]
        public async Task<IActionResult> GenerateRcbList([FromBody] GetRcbListQuery command)
        {
            return Ok(await _mediator.Send(command));
        }

        [HttpGet("Ping")]
        public Task<IActionResult> Ping()
        {
            return Task.FromResult(new OkResult() as IActionResult);
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using zone.bankconnector.moniepoint.Interfaces;
using zone.bankconnector.moniepoint.Models;

namespace zone.bankconnector.moniepoint.Controllers
{
    [ApiController]
    //[Route("api/v1/transfer")]
    [Produces("application/json")]
    public class MonieTransferController : ControllerBase
    {
        private readonly ILogger<MonieTransferController> _logger;
        protected IConnector _connector;
        public MonieTransferController(IConnector connector, ILogger<MonieTransferController> logger)
        {
            _connector = connector;
            _logger = logger;
        }

        [HttpPost]
        [Route("nameinquiry")]
        [ProducesDefaultResponseType(typeof(NameInquiryResponse))]
        public async Task<IActionResult> Inquiry([FromBody] NameEnquiryRequest request)
        {
            return Ok(await _connector.NameEnquiryAsync(request));
        }

        [HttpPost]
        [Route("transfer")]
        [ProducesDefaultResponseType(typeof(GenericResponse<TransferResponse>))]
        public async Task<IActionResult> Transfer([FromBody] TransferRequest request)
        {
            return Ok(await _connector.IntraBankAsync(request));
        }

        [HttpGet]
        [Route("transfer/status")]
        [ProducesDefaultResponseType(typeof(GenericResponse<TransactionStatusResponse>))]
        public async Task<IActionResult> Status([FromQuery] TransactionStatusRequest request)
        {
            return Ok(await _connector.IntraBankStatusAsync(request));
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using zone.bankconnector.moniepoint.Controllers;
using zone.bankconnector.moniepoint.Interfaces;
using zone.bankconnector.moniepoint.Models;

namespace zone.bankconnector.moniepoint.Tests;

public class MonieTransferControllerTests
{
    private static MonieTransferController BuildController(Mock<IConnector> connector)
    {
        return new MonieTransferController(connector.Object,
            NullLogger<MonieTransferController>.Instance);
    }


    [Fact]
    public async Task Inquiry_ReturnsOk_WithConnectorResult()
    {
        var expected = new NameInquiryResponse
        {
            ResponseCode = "00",
            AccountName  = "Jane Smith",
            AccountNumber= "9876543210"
        };
        var connector = new Mock<IConnector>();
        connector.Setup(c => c.NameEnquiryAsync(It.IsAny<NameEnquiryRequest>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(expected);

        var controller = BuildController(connector);
        var actionResult = await controller.Inquiry(new NameEnquiryRequest { AccountNumber = "9876543210" });

        var ok = Assert.IsType<OkObjectResult>(actionResult);
        var body = Assert.IsType<NameInquiryResponse>(ok.Value);
        Assert.Equal("00",          body.ResponseCode);
        Assert.Equal("Jane Smith",  body.AccountName);
        Assert.Equal("9876543210",  body.AccountNumber);
    }

    [Fact]
    public async Task Inquiry_PassesRequestToConnector()
    {
        NameEnquiryRequest? captured = null;
        var connector = new Mock<IConnector>();
        connector.Setup(c => c.NameEnquiryAsync(It.IsAny<NameEnquiryRequest>(), It.IsAny<CancellationToken>()))
                 .Callback<NameEnquiryRequest, CancellationToken>((r, _) => captured = r)
                 .ReturnsAsync(new NameInquiryResponse());

        var controller = BuildController(connector);
        await controller.Inquiry(new NameEnquiryRequest
        {
            AccountNumber       = "0123456789",
            DestinationBankCode = "000013",
            TransactionReference= "APT00015ABCDEF123456"
        });

        Assert.NotNull(captured);
        Assert.Equal("0123456789",           captured!.AccountNumber);
        Assert.Equal("000013",               captured.DestinationBankCode);
        Assert.Equal("APT00015ABCDEF123456", captured.TransactionReference);
    }


    [Fact]
    public async Task Transfer_ReturnsOk_WithConnectorResult()
    {
        var expected = new GenericResponse<TransferResponse>
        {
            ResponseCode = "00",
            Data = new TransferResponse
            {
                ResponseCode         = "00",
                TransactionReference = "PAY-2024-001",
                Status               = "00"
            }
        };
        var connector = new Mock<IConnector>();
        connector.Setup(c => c.IntraBankAsync(It.IsAny<TransferRequest>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(expected);

        var controller   = BuildController(connector);
        var actionResult = await controller.Transfer(new TransferRequest
        {
            ToAccount            = "0123456789",
            AmountToDebit        = 500000,
            TransactionReference = "APT00015260424FT0001"
        });

        var ok = Assert.IsType<OkObjectResult>(actionResult);
        var body = Assert.IsType<GenericResponse<TransferResponse>>(ok.Value);
        Assert.Equal("00",          body.ResponseCode);
        Assert.Equal("PAY-2024-001", body.Data!.TransactionReference);
    }

    [Fact]
    public async Task Transfer_PassesRequestToConnector()
    {
        TransferRequest? captured = null;
        var connector = new Mock<IConnector>();
        connector.Setup(c => c.IntraBankAsync(It.IsAny<TransferRequest>(), It.IsAny<CancellationToken>()))
                 .Callback<TransferRequest, CancellationToken>((r, _) => captured = r)
                 .ReturnsAsync(new GenericResponse<TransferResponse>());

        var controller = BuildController(connector);
        var request = new TransferRequest
        {
            ToAccount            = "0123456789",
            AmountToDebit        = 100000,
            TransactionReference = "APT00015XYZXYZ123456",
            Narration            = "Rent payment"
        };

        await controller.Transfer(request);

        Assert.NotNull(captured);
        Assert.Equal("0123456789",           captured!.ToAccount);
        Assert.Equal(100000L,                captured.AmountToDebit);
        Assert.Equal("APT00015XYZXYZ123456", captured.TransactionReference);
        Assert.Equal("Rent payment",         captured.Narration);
    }


    [Fact]
    public async Task Status_ReturnsOk_WithConnectorResult()
    {
        var expected = new GenericResponse<TransactionStatusResponse>
        {
            ResponseCode = "00",
            Data = new TransactionStatusResponse { ResponseCode = "00", Status = "00" }
        };
        var connector = new Mock<IConnector>();
        connector.Setup(c => c.IntraBankStatusAsync(It.IsAny<TransactionStatusRequest>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(expected);

        var controller   = BuildController(connector);
        var actionResult = await controller.Status(new TransactionStatusRequest
        {
            TransactionReference = "APT00015260424TSQ001"
        });

        var ok   = Assert.IsType<OkObjectResult>(actionResult);
        var body = Assert.IsType<GenericResponse<TransactionStatusResponse>>(ok.Value);
        Assert.Equal("00", body.ResponseCode);
        Assert.Equal("00", body.Data!.Status);
    }

    [Fact]
    public async Task Status_PassesReferenceToConnector()
    {
        TransactionStatusRequest? captured = null;
        var connector = new Mock<IConnector>();
        connector.Setup(c => c.IntraBankStatusAsync(It.IsAny<TransactionStatusRequest>(), It.IsAny<CancellationToken>()))
                 .Callback<TransactionStatusRequest, CancellationToken>((r, _) => captured = r)
                 .ReturnsAsync(new GenericResponse<TransactionStatusResponse>());

        var controller = BuildController(connector);
        await controller.Status(new TransactionStatusRequest
        {
            TransactionReference = "APT00015260424TSQ999"
        });

        Assert.NotNull(captured);
        Assert.Equal("APT00015260424TSQ999", captured!.TransactionReference);
    }
}

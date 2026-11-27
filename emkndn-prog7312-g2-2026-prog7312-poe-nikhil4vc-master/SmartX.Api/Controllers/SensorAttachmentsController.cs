using Microsoft.AspNetCore.Mvc;
using SmartX.Core.DTOs;
using SmartX.Core.Exceptions;
using SmartX.Core.Interfaces;

namespace SmartX.Api.Controllers
{
    [ApiController]
    [Route("api/sensors/{sensorId:guid}/attachments")]
    public class SensorAttachmentsController : ControllerBase
    {
        private readonly ISensorAttachmentService
            _attachmentService;

        public SensorAttachmentsController(
            ISensorAttachmentService attachmentService)
        {
            _attachmentService = attachmentService;
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(11_000_000)]
        public async Task<ActionResult<
            SensorAttachmentResponse>> Upload(
            Guid sensorId,
            [FromForm] IFormFile file,
            CancellationToken cancellationToken)
        {
            if (file is null)
            {
                return BadRequest(new
                {
                    message = "An attachment is required."
                });
            }

            try
            {
                await using var content =
                    file.OpenReadStream();

                var response =
                    await _attachmentService.UploadAsync(
                        sensorId,
                        content,
                        file.FileName,
                        file.ContentType,
                        file.Length,
                        cancellationToken);

                return StatusCode(
                    StatusCodes.Status201Created,
                    response);
            }
            catch (SensorNotFoundException exception)
            {
                return NotFound(new
                {
                    message = exception.Message
                });
            }
            catch (InvalidAttachmentException exception)
            {
                return BadRequest(new
                {
                    message = exception.Message
                });
            }
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<
            SensorAttachmentResponse>>> GetAll(
            Guid sensorId,
            CancellationToken cancellationToken)
        {
            try
            {
                var attachments =
                    await _attachmentService.GetForSensorAsync(
                        sensorId,
                        cancellationToken);

                return Ok(attachments);
            }
            catch (SensorNotFoundException exception)
            {
                return NotFound(new
                {
                    message = exception.Message
                });
            }
        }

        [HttpGet("{attachmentId:guid}/download")]
        public async Task<IActionResult> Download(
            Guid sensorId,
            Guid attachmentId,
            CancellationToken cancellationToken)
        {
            var download =
                await _attachmentService.DownloadAsync(
                    sensorId,
                    attachmentId,
                    cancellationToken);

            if (download is null)
            {
                return NotFound(new
                {
                    message = "Attachment not found."
                });
            }

            return File(
                download.Content,
                download.ContentType,
                download.FileName,
                enableRangeProcessing: false);
        }
    }
}


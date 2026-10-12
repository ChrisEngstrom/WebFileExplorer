using Microsoft.AspNetCore.Mvc;
using WebFileExplorer.Application.Abstractions.Messaging;
using WebFileExplorer.Application.Configurations;
using WebFileExplorer.Application.FileSystemManagement.Commands.UploadFile;
using WebFileExplorer.Application.FileSystemManagement.Queries.BrowseFileSystem;
using WebFileExplorer.Application.FileSystemManagement.Queries.DownloadFileItem;
using WebFileExplorer.Application.FileSystemManagement.Queries.SearchFileSystem;
using WebFileExplorer.Application.Responses;
using WebFileExplorer.SharedKernel;

namespace WebFileExplorer.WebApp.Controllers;

[ApiController]
[Route("api/files")]
public sealed class FilesController : ControllerBase
{
    private readonly IQueryHandler<BrowseQuery, FileSystemItemsResponse> _browse;
    private readonly IQueryHandler<SearchQuery, FileSystemItemsResponse> _search;
    private readonly IQueryHandler<DownloadQuery, DownloadResponse> _download;
    private readonly ICommandHandler<UploadCommand> _upload;

    public FilesController(
        IQueryHandler<BrowseQuery, FileSystemItemsResponse> browse,
        IQueryHandler<SearchQuery, FileSystemItemsResponse> search,
        IQueryHandler<DownloadQuery, DownloadResponse> download,
        ICommandHandler<UploadCommand> upload)
    {
        _browse = browse;
        _search = search;
        _download = download;
        _upload = upload;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string path = "", [FromQuery] string? q = null)
    {
        // When no query is present we list all children. When a query is provided we search recursively.
        Result<FileSystemItemsResponse> result = q is null
            ? await _browse.Handle(new BrowseQuery(path), HttpContext.RequestAborted)
            : await _search.Handle(new SearchQuery(path, q), HttpContext.RequestAborted);

        return result.IsFailure ? ErrorResponse(result.Error) : Ok(result.Value);
    }

    [HttpGet("download")]
    public async Task<IActionResult> Download([FromQuery] string path)
    {
        Result<DownloadResponse> result = await _download.Handle(new DownloadQuery(path), HttpContext.RequestAborted);

        if (result.IsFailure)
        {
            return ErrorResponse(result.Error);
        }

        return File(result.Value.Content, "application/octet-stream", result.Value.Name);
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload(
        [FromQuery] string? path,
        [FromForm] IFormFile file,
        [FromServices] AppConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        string directory = path ?? string.Empty;

        if (file is null)
        {
            return BadRequest(new { error = "Select a file." });
        }

        await using Stream content = file.OpenReadStream();
        Result uploadCommandResult = await _upload.Handle(
            new UploadCommand(
                directory,
                file.FileName,
                content),
            HttpContext.RequestAborted);
        ObjectResult result;

        if (uploadCommandResult.IsFailure)
        {
            if (uploadCommandResult.Error.Type == ErrorType.LimitExceeded)
            {
                result = StatusCode(413, new
                {
                    error = uploadCommandResult.Error.Description,
                    code = uploadCommandResult.Error.Code,
                    maxUploadBytes = configuration.MaxUploadBytes,
                    requestTooLarge = false
                });
            }
            else
            {
                result = ErrorResponse(uploadCommandResult.Error);
            }
        }
        else
        {
            // After a successful upload, point to the new file's download route.
            string relativePath = string.IsNullOrEmpty(directory)
                ? file.FileName
                : $"{directory}/{file.FileName}";

            var location = new Uri(
                $"/api/files/download?path={Uri.EscapeDataString(relativePath)}",
                UriKind.Relative);

            result = Created(location, null);
        }

        return result;
    }

    /// <summary>
    /// Keep HTTP status selection here. The Application returns transport-independent errors.
    /// </summary>
    /// <param name="error">The Application error to translate.</param>
    /// <returns>The proper HTTP status based on the Application error.</returns>
    private ObjectResult ErrorResponse(Error error) => error.Type switch
    {
        ErrorType.Validation => BadRequest(new { error = error.Description, code = error.Code }),
        ErrorType.NotFound => NotFound(new { error = error.Description, code = error.Code }),
        ErrorType.Conflict => Conflict(new { error = error.Description, code = error.Code }),
        ErrorType.LimitExceeded => StatusCode(413, new { error = error.Description, code = error.Code }),
        _ => StatusCode(500, new { error = "Unexpected error." }),
    };
}
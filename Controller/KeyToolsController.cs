using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Paymentcardtools.Extension.Interface;
using Paymentcardtools.Models.DataModel;
using Paymentcardtools.Service.Interface;

namespace Paymentcardtools.Controller;

[ApiController]
[Route("api/[controller]")]
public class KeyToolsController : ControllerBase
{
    private readonly IKeyQualityService _keyQualityService;
    private readonly IInputSource _inputSource;

    public KeyToolsController(IKeyQualityService keyQualityService, IInputSource inputSource)
    {
        _keyQualityService = keyQualityService;
        _inputSource = inputSource;
    }

    [HttpPost("key")]
    public ActionResult<KeyCheckResult> ValidateSingle([FromBody] KeyRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Key))
        {
            return BadRequest("Bắt buộc phải có key");
        }

        _keyQualityService.SetAlgorithm(request.Algorithm);

        var result = _keyQualityService.ValidateKey(request.Key.Trim());
        return Ok(result);
    }

    [HttpPost("keys")]
    public ActionResult<BatchKeyCheckResult> ValidateBatch([FromBody] BatchKeyRequest request)
    {
        if (request?.Keys == null || request.Keys.Count == 0)
        {
            return BadRequest("Ít nhất một key phải tồn tại");
        }

        _keyQualityService.SetAlgorithm(request.Algorithm);

        var result = _keyQualityService.ValidateBatch(request.Keys);
        return Ok(result);
    }

    [HttpPost("file")]
    public async Task<ActionResult<BatchKeyCheckResult>> ValidateFile(
    [FromForm] FileUploadRequest request)
    {
        if (request.File == null || request.File.Length == 0)
        {
            return BadRequest("File rỗng");
        }

        await using var stream = request.File.OpenReadStream();
        var keys = await _inputSource.ExtractKeyAsync(stream);

        if (keys.Count == 0)
        {
            return BadRequest("Không tìm thấy key nào trong file.");
        }

        _keyQualityService.SetAlgorithm(request.Algorithm);
        var result = _keyQualityService.ValidateBatch(keys.Select(k => k.KeyHex).ToList());
        return Ok(result);
    }
}

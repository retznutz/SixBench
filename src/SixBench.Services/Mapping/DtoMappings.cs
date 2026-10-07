using SixBench.Common.Dtos;
using SixBench.Data.Entities;

namespace SixBench.Services.Mapping;

/// <summary>
/// Entity → DTO conversions.
/// </summary>
public static class DtoMappings
{
    /// <summary>Maps a <see cref="RokuDevice"/> to its DTO.</summary>
    /// <param name="entity">The entity.</param>
    /// <returns>The DTO.</returns>
    public static RokuDeviceDto ToDto(this RokuDevice entity) => new(
        entity.Id,
        entity.SerialNumber,
        entity.FriendlyName,
        entity.Model,
        entity.IpAddress,
        entity.Port,
        entity.IsManual,
        DateTime.SpecifyKind(entity.LastSeenUtc, DateTimeKind.Utc));

    /// <summary>Maps an <see cref="EncoderLink"/> to its DTO.</summary>
    /// <param name="entity">The entity (with <see cref="EncoderLink.RokuDevice"/> loaded when linked).</param>
    /// <returns>The DTO.</returns>
    public static EncoderLinkDto ToDto(this EncoderLink entity) => new(
        entity.Id,
        entity.CaptureDeviceStableId,
        entity.DisplayName,
        entity.VideoInput,
        entity.AudioInput,
        entity.RokuDeviceId,
        entity.RokuDevice?.ToDto(),
        entity.AllowDeviceAudio,
        entity.FrameRate,
        entity.VideoSize,
        entity.PixelFormat,
        DateTime.SpecifyKind(entity.CreatedUtc, DateTimeKind.Utc),
        DateTime.SpecifyKind(entity.UpdatedUtc, DateTimeKind.Utc));
}

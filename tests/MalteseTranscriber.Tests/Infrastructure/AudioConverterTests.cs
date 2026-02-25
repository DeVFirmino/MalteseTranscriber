using FluentAssertions;
using MalteseTranscriber.Infrastructure;

namespace MalteseTranscriber.Tests.Infrastructure;

public class AudioConverterTests
{
    [Fact]
    public void PcmToWav_Should_ProduceValidWavHeader_When_GivenPcmData()
    {
        // Arrange
        var pcm = new byte[3200]; // 100ms at 16kHz/16bit/mono

        // Act
        var wav = AudioConverter.PcmToWav(pcm);

        // Assert
        wav.Should().NotBeNullOrEmpty();
        wav.Length.Should().Be(44 + pcm.Length); // 44-byte header + data
    }

    [Fact]
    public void PcmToWav_Should_StartWithRiffHeader_When_GivenAnyInput()
    {
        // Arrange
        var pcm = new byte[100];

        // Act
        var wav = AudioConverter.PcmToWav(pcm);

        // Assert — RIFF....WAVE
        System.Text.Encoding.ASCII.GetString(wav, 0, 4).Should().Be("RIFF");
        System.Text.Encoding.ASCII.GetString(wav, 8, 4).Should().Be("WAVE");
    }

    [Fact]
    public void PcmToWav_Should_SetCorrectFileSize_When_GivenPcmData()
    {
        // Arrange
        var pcm = new byte[1000];

        // Act
        var wav = AudioConverter.PcmToWav(pcm);

        // Assert — bytes 4-7 contain (36 + data length)
        var fileSize = BitConverter.ToInt32(wav, 4);
        fileSize.Should().Be(36 + pcm.Length);
    }

    [Fact]
    public void PcmToWav_Should_SetPcmFormat_When_UsingDefaults()
    {
        // Arrange & Act
        var wav = AudioConverter.PcmToWav(new byte[100]);

        // Assert — bytes 20-21 = audio format (1 = PCM)
        var format = BitConverter.ToInt16(wav, 20);
        format.Should().Be(1);
    }

    [Fact]
    public void PcmToWav_Should_SetSampleRate_When_Using16kHz()
    {
        // Arrange & Act
        var wav = AudioConverter.PcmToWav(new byte[100], sampleRate: 16000);

        // Assert — bytes 24-27 = sample rate
        var sampleRate = BitConverter.ToInt32(wav, 24);
        sampleRate.Should().Be(16000);
    }

    [Fact]
    public void PcmToWav_Should_HandleEmptyInput_When_PcmIsEmpty()
    {
        // Arrange & Act
        var wav = AudioConverter.PcmToWav(Array.Empty<byte>());

        // Assert
        wav.Length.Should().Be(44); // header only
    }

    [Fact]
    public void PcmToWav_Should_PreservePcmData_When_AppendedAfterHeader()
    {
        // Arrange
        var pcm = new byte[] { 0x01, 0x02, 0x03, 0x04 };

        // Act
        var wav = AudioConverter.PcmToWav(pcm);

        // Assert — last 4 bytes should match input
        wav[44].Should().Be(0x01);
        wav[45].Should().Be(0x02);
        wav[46].Should().Be(0x03);
        wav[47].Should().Be(0x04);
    }
}

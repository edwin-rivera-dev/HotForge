using HotForge.Core.Model;
using HotForge.Linux;
using Xunit;

namespace HotForge.Core.Tests;

public class LinuxKeyMapTests
{
    [Theory]
    [InlineData(35, Key.H)]
    [InlineData(30, Key.A)]
    [InlineData(50, Key.M)]
    [InlineData(2, Key.D1)]
    [InlineData(11, Key.D0)]
    [InlineData(59, Key.F1)]
    [InlineData(88, Key.F12)]
    [InlineData(57, Key.Space)]
    [InlineData(28, Key.Enter)]
    [InlineData(999, Key.None)]
    public void FromCode_maps_evdev_codes_to_canonical_keys(int code, Key expected)
        => Assert.Equal(expected, LinuxKeyMap.FromCode(code));

    [Theory]
    [InlineData(29, KeyModifiers.Ctrl)]
    [InlineData(97, KeyModifiers.Ctrl)]
    [InlineData(56, KeyModifiers.Alt)]
    [InlineData(42, KeyModifiers.Shift)]
    [InlineData(125, KeyModifiers.Meta)]
    [InlineData(35, KeyModifiers.None)]
    public void ModifierFor_classifies_modifier_codes(int code, KeyModifiers expected)
        => Assert.Equal(expected, LinuxKeyMap.ModifierFor(code));

    [Fact]
    public void TryFromChar_lowercase_letter_has_no_shift()
    {
        Assert.True(LinuxKeyMap.TryFromChar('h', out int code, out bool shift));
        Assert.Equal(35, code);
        Assert.False(shift);
    }

    [Fact]
    public void TryFromChar_uppercase_letter_requests_shift_same_code()
    {
        Assert.True(LinuxKeyMap.TryFromChar('H', out int code, out bool shift));
        Assert.Equal(35, code);
        Assert.True(shift);
    }

    [Fact]
    public void TryFromChar_rejects_unsupported_character()
        => Assert.False(LinuxKeyMap.TryFromChar('€', out _, out _));
}

package bech32cosmos_test

import (
	"strings"
	"testing"

	"github.com/cosmos/bech32cosmos/go"
)

func TestBech32(t *testing.T) {
	tests := []struct {
		str   string
		valid bool
	}{
		{"A:2UEL5L", true},
		{"an83characterlonghumanreadablepartthatcontainsthenumber1andtheexcludedcharactersbio:tt5tgs", true},
		{"abcdef:qpzry9x8gf2tvdw0s3jn54khce6mua7lmqqqxw", true},
		{"1:qqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqc8247j", true},
		{"split:checkupstagehandshakeupstreamerranterredcaperred2y9e3w", true},
		{"split:checkupstagehandshakeupstreamerranterredcaperred2y9e2w", false},                                // invalid checksum
		{"s lit:checkupstagehandshakeupstreamerranterredcaperredp8hs2p", false},                                // invalid character (space) in hrp
		{"spl" + string(127) + "t:checkupstagehandshakeupstreamerranterredcaperred2y9e3w", false},              // invalid character (DEL) in hrp
		{"split:cheo2y9e2w", false},                                                                            // invalid character (o) in data part
		{"split:a2y9w", false},                                                                                 // too short data part
		{":checkupstagehandshakeupstreamerranterredcaperred2y9e3w", false},                                     // empty hrp
		{"1:qqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqsqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqc8247j", false}, // too long
	}

	for _, test := range tests {
		str := test.str
		hrp, decoded, err := bech32cosmos.Decode(str)
		if !test.valid {
			// Invalid string decoding should result in error.
			if err == nil {
				t.Error("expected decoding to fail for "+
					"invalid string %v", test.str)
			}
			continue
		}

		// Valid string decoding should result in no error.
		if err != nil {
			t.Errorf("expected string to be valid bech32: %v", err)
		}

		// Check that it encodes to the same string
		encoded, err := bech32cosmos.Encode(hrp, decoded)
		if err != nil {
			t.Errorf("encoding failed: %v", err)
		}

		if encoded != strings.ToLower(str) {
			t.Errorf("expected data to encode to %v, but got %v",
				str, encoded)
		}

		// Flip a bit in the string an make sure it is caught.
		pos := strings.LastIndexAny(str, "1")
		flipped := str[:pos+1] + string((str[pos+1] ^ 1)) + str[pos+2:]
		_, _, err = bech32cosmos.Decode(flipped)
		if err == nil {
			t.Error("expected decoding to fail")
		}
	}
}

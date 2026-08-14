using System;

namespace TSS.Gateway.Sdk
{
    // The error a gateway operation surfaces to the caller: the player answered with an error
    // response, carrying the protocol error code it raised.
    public class GatewayException : Exception
    {
        public GatewayException(int code, string message) : base(message) => Code = code;

        public int Code { get; }
    }
}

namespace TSS.Gateway.Sdk
{
    // Addressing scope of a request routing key. Screen targets one screen (the full
    // location.setup.screen key), Setup targets every screen of the setup, Location targets every
    // screen at the location.
    public enum RequestScope
    {
        Screen,
        Setup,
        Location
    }
}

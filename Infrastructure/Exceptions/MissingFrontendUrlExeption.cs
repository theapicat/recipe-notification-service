namespace Infrastructure.Exceptions;

public class MissingFrontendUrlException()
    : Exception("Kritisk konfigurasjonsfeil: 'FrontendUrl' er ikke angitt i AppSettings eller miljøvariabler.");
using Microsoft.AspNetCore.SignalR;

namespace Orion.Api.Hubs;

// This hub uses the user's ID from the JWT token for routing messages.
// When a client connects, SignalR will automatically associate the connection
// with the User ID from their authentication token.
public class OrderStatusHub : Hub
{
    // We don't need to add any methods here for our use case.
    // Client applications (like a browser) won't be calling methods on the server.
    // The server will just use this hub's context to push messages *out* to clients.
}
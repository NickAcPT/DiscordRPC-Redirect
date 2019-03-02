# DiscordRPC-Redirect
Proof-of-concept tool that forwards the Discord Rich Presence named pipe on Windows.

# How does it work?
First of all, in order to make it simple to understand, I'm going to use the term `host` for the machine that has the desktop Discord client installed and running and the term `guest` for the machine that is going to be using this app to forward the RPC into Discord. 

Now that's out of the way, let's start:
- First, we start the server on the host. This is make the app into a named pipe client that will be receiving data from the Discord app.
- Second, we run the app on the guest and tell it what's the target host in order to connect.
- After the devices are connected, the app on the guest device will start a named pipe server that'll be forwarding the bytes from the guest to the host.

# How to use?
This is still a bit unstable since it's a PoC, but you just need to compile it and run the final exe.

On start, you'll be prompted with this screen:

![](https://i.imgur.com/InhAGLD.png)

If you reply `Y`, the app will assume the host configuration and try to connect to the RPC named pipe.

You'll be then asked what is the pipe number. The default is `0` (you just need to press Enter) and unless you have more than one client opened up, you can let it use the default value.

# To-Didn't Do
- Handle exceptions better

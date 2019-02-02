
# Incursion-dotnet

A dotnet core; XMPP bot developed for the incursion squad for the [Eve Online](https://eveonline.com) alliance [Goonswarm Federation](https://goonfleet.com).

Use the [repository issues](https://github.com/mlohstroh/incursion-dotnet/issues) system to file bug reports or feature requests. If you do not have a github account you can contact [samuel_the_terrible](xmpp:samuel_the_terrible@goonfleet.com).

### Installation
Application Requirements

- Redis database
- 100Mb of RAM

1. Grab the [latest release](https://github.com/mlohstroh/incursion-dotnet/releases).
2. Extract the content of the zip file into the working directory.
3. Create an .ENV file and fill out the vales _([See Below](#environment-variables))_.
3. Create a systemd.service _([See Below](#systemctl-service---for-ubuntu))_.
4. Enable the service and reboot the box to test it functions correctly `sudo reboot`.


#### Environment Variables
The application requires a `.env` file in the root directory. 

`JABBER_USERNAME` & `JABBER_PASSWORD` are used by the bot to login to the jabber server. If you use GSF jabber use your [Goonfleet ESA](https://goonfleet.com/esa/) details.

The `JABBER_NICKNAME` gives the bot it's own user alias in channel lists. In private messages the bot will still use the value set as the `JABBER_USERNAME`.


`SEC_STATUS_CEILING`
is used to filter incursions displayed through the bot. The formula used is `<= SEC_STATUS_CEILING -1 `. This float can be set to any value. However you may wish to use one of the values listed below:

- 1.1 - Highsec/Lowsec/Nullsec.
- 0.5 - Lowsec/Nullsec.
- 0.1 - Nullsec Only

The default values have been set in the .env example before. Simply add a jabber username and password.
```
JABBER_USERNAME=<<esa_user_name>>
JABBER_PASSWORD=<<esa_password>>
JABBER_DOMAIN=goonfleet.com
JABBER_NICKNAME=Sansha_Kuvakei
REDIS_URL_PORT=localhost:6379
HTTP_PORT=80
SEC_STATUS_CEILING=0.5
```

#### Systemctl service - for Ubuntu
To run the bot as a deamon service you will need to make a systemd.service as outlined below. A basic service is included below, should you wish to expand on it you can find the [documentation here](https://www.digitalocean.com/community/tutorials/understanding-systemd-units-and-unit-files). 

The following systemctl options are available `start`, `stop`, `restart`, `enabled`, `disabled` & `status`.
1. Create a file called `incursionbot.service` in the `/lib/systemd/system/` directory.
2. Run the command `systemctl daemon-reload`


```[Unit]
Description="Incursion Squad jabber bot"
Documentation=https://github.com/mlohstroh/incursion-dotnet

[Service]
Type=simple

WorkingDirectory=/opt/incursion-bot/run/publish
ExecStart=/opt/incursion-bot/image/scripts/launch

RestartSec=2
Restart=on-failure

[Install]
WantedBy=multi-user.target
```


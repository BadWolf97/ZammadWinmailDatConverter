# ZammadWinmailDatConverter
A little tool connecting to the [Zammad](https://github.com/zammad/zammad) API, downloading new winmail.dat-Attachments (coded as TNEF) and adding the containing attachments as a new article.

The tool is running as an external application, periodically checking via the Zammad-API for new tickets and articles. It only checks tickets not in state 4 (default for closed). If a new article with a winmail.dat-attachment is found, the tool will download the attachment, extract the contained files and add them as a new article to the ticket.

## Configuration
The configuration is done via environment variables. The following variables are available:
- `ZammadHost`: The URL of the Zammad instance
- `ZammadToken`: The API-Token for the Zammad instance

## Usage
The tool is running as a console application. It will check for new articles every 1 minute. If a new article with a winmail.dat-attachment is found, the tool will download the attachment, extract the contained files and add them as a new article to the ticket.

# Contributing
Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change.

# License
[MIT](https://choosealicense.com/licenses/mit/)

# Contact
If you have any questions, feel free to contact me or open an issue.

# Thanks
- to the Zammad-Team for providing a great helpdesk software. 
- to the authors of the library ([MimeKit](https://github.com/jstedfast/MimeKit)) used in this project.
- to my employer [iRFP](https://www.irfp.de) for giving me the time to work on this project and allowing me to publish it as open source.
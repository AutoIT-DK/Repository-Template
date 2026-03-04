# Finish setup

To finish setting up this solution, you need to enter valid github credentials into nuget.config. You need to add this under 

<configuraton>
...
<packageSourceCredentials>
		<github>
			<add key="Username" value="<your github username, e.g. mgryning>" />
			<add key="ClearTextPassword" value="<Your Personal github access token>" />
		</github>
	</packageSourceCredentials>
...
</configuration>

To make a new github access token, go to Github -> Your user -> Settings -> Developer Settings -> Personal Access tokens -> Tokens Classic -> Create one. Then note the tokenid, and also click Configure SSO

# Notes

- Security for this solution is setup for localhost:4000 and localhost:5000. If you change the port, the identityserver cannot redirect unless you add the redirecturl in identityadmin.autoit.dk for client 50100. When deploying to production, remember to add the redirecturl for the client, or preferably ask Kristoffer to set up a new identityclient with proper redirecturl

- Deployment to remote servers can we done using github actions. Add with "Add deployment via autoit mcp". This is far the easiets when it's setup properly
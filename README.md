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
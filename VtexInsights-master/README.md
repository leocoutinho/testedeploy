VtexInsights
====================
[Azure](https://azure.microsoft.com/en-us/) easily deploy of your own [Vtex](http://vtex.com) data BI environment 

Prerequisite
====================
To use this project you will need:

- [Microsoft Azure Subscription](https://github.com/vtex/VtexInsights/wiki/Azure-Account-Creation)
- [Microsoft PowerBi Service](https://github.com/vtex/VtexInsights/wiki/Power-BI-Service)
- [Vtex "Account", "AppKey" and "AppToken"](https://github.com/vtex/VtexInsights/wiki/VTEX-Configurations)

Deploy the application
=======================
To deploy the application on Microsoft Azure is really simple, use the button bellow and follow the wizard, to help with the wizard check the document [Vtex Insights Deployment](https://github.com/vtex/VtexInsights/wiki/App-Deployment-to-Azure)

[![Deploy to Azure](http://azuredeploy.net/deploybutton.png)](https://azuredeploy.net/)

How it works
====================
Clicking **Deploy** button above a web application will be deployed on your Azure enviroment and also all necessary Azure resources will be created.

The Web Application runs data-mining routines to pull data from Vtex OMS feed Api and storage it in your Microsoft Azure Event Hub processed by Azure Stream Analytics and then will land to Microsoft PowerBi to provide you the a live reports.

The Step-by-step is availble in this Wiki:[Event Hub + Stream Analytics](https://github.com/vtex/VtexInsights/wiki/Stream-Analytics)

The whole project was designed to use the minimum resources from your Azure Subscription, meaning that you will pay the less possible to host your BI environment.

Security
====================
**Hey, Don't Worry !!** Everything will be deployed into your own Azure subscription so just you (or allowed ones) can access your data.

What will be created in your Microsoft Azure Subscription
============================
- Azure Hosting Standard S1 (need always on)
- Azure Web App 
- Azure Event Hub (Service Bus)
- Azure Stream Analytics (for realtime report)
- Application Insights

You can check what will be created using the file [azuredeploy.json](https://github.com/vtex/VtexInsights/blob/azure_easydeploy/azuredeploy.json), this file is a Azure Template that descrives all the infrastructure needed.


Pos-Deployment Steps
================
When you application is deployed, it will be in **Demostration Mode**. As soon as the deployment has ended it will start to generate data (10 order records per minute, with different status)

This mode will allow you to confirm that your Azure Infrastructure is up and running and also perform the manual steps needed to have the whole infrastructure before start to pool your Vtex OMS data.

All these steps is available at the wiki session [Wiki Home Page](https://github.com/vtex/VtexInsights/wiki)






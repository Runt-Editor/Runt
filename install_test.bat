call kpm restore
echo "Package restore done..."
pause

cd "%~dp0\src\Runt.Web"
call npm install
echo "NPM install done..."
pause

call bower install
echo "Bower install done..."
pause

call gulp
echo "Gulp done..."
pause

cd "%~dp0"
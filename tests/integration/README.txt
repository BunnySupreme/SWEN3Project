to run the curl script integration tests in WSL, first run this

sed -i 's/\r$//' integrationtests.sh

This is to ensure Linux does not read the Windows line endings like \r or \n
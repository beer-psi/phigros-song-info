set -e

if curl "https://itunes.apple.com/lookup?entity=software,iPadSoftware&limit=1&media=software&bundleId=games.Pigeon.Phigros" \
       | jq -r .results[0].version \
       | tr -d "\r" \
       | cmp -s docs/.phigros-version -; then
    echo "Phigros wasn't updated."
    exit 0
fi

mkdir -p work
pushd work || exit 1

curl -LO https://github.com/majd/ipatool/releases/download/v2.1.3/ipatool-2.1.3-linux-amd64.tar.gz
tar xzvf ipatool-2.1.3-linux-amd64.tar.gz 
sudo install -Dm755 bin/ipatool-2.1.3-linux-amd64 /usr/local/bin/ipatool
rm -r bin ipatool-2.1.3-linux-amd64.tar.gz

pip install UnityPy

ipatool auth login -e "$1" -p "$2" --keychain-passphrase "grass" --non-interactive
ipatool download -b games.Pigeon.Phigros -o phigros.ipa --keychain-passphrase "grass" --non-interactive

python ../struct_dumper.py phigros.ipa

popd || exit 1

dotnet run -- work/GameInformation.bin work/TipsProvider.bin

curl "https://itunes.apple.com/lookup?entity=software,iPadSoftware&limit=1&media=software&bundleId=games.Pigeon.Phigros" \
    | jq -r .results[0].version \
    | tr -d "\r" > docs/.phigros-version

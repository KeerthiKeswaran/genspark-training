## File for building ubuntu:22.04 and installing required packages for linux-day-2 lab:

FROM ubuntu:22.04

# Remove all extra sources
RUN rm -rf /etc/apt/sources.list.d/*

# Set mirror based on architecture and OS version
RUN . /etc/os-release \
 && arch=$(dpkg --print-architecture) \
 && if [ "$arch" = "amd64" ]; then \
        repo="http://mirror.cse.iitk.ac.in/ubuntu"; \
    else \
        repo="http://mirrors.aliyun.com/ubuntu-ports"; \
    fi \
 && echo "deb $repo $VERSION_CODENAME main restricted universe multiverse" > /etc/apt/sources.list \
 && echo "deb $repo $VERSION_CODENAME-updates main restricted universe multiverse" >> /etc/apt/sources.list \
 && echo "deb $repo $VERSION_CODENAME-backports main restricted universe multiverse" >> /etc/apt/sources.list \
 && echo "deb $repo $VERSION_CODENAME-security main restricted universe multiverse" >> /etc/apt/sources.list

# Clean + update
RUN apt-get clean \
 && rm -rf /var/lib/apt/lists/* \
 && apt-get update

# Install packages
RUN apt-get install -y \
    git curl wget nano ca-certificates gnupg lsb-release software-properties-common \
 && apt-get clean \
 && rm -rf /var/lib/apt/lists/*

CMD ["bash"]
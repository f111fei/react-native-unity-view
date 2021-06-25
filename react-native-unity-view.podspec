require 'json'

package = JSON.parse(File.read(File.join(__dir__, 'package.json')))

Pod::Spec.new do |s|
  s.name           = 'react-native-unity-view'
  s.version        = package['version']
  s.summary        = package['description']
  s.description    = package['description']
  s.license        = package['license']
  s.author         = package['author']
  s.homepage       = package['homepage']
  s.source         = { :git => 'https://github.com/coder89/react-native-unity-view', :tag => "v#{s.version}" }

  s.requires_arc   = true
  s.platform       = :ios, '9.0'

  s.subspec "RN" do |ss|
    ss.source_files = "ios/**/*.{h,m}"
  end

  s.default_subspecs = "RN"

  s.preserve_paths = 'LICENSE', 'README.md', 'package.json', 'index.js'

  s.dependency 'React-Core'
end

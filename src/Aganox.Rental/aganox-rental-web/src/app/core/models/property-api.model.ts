import {
  Property,
  PropertyStatus,
  PropertyType
} from '../models/property.model';

export interface PropertyAddressApiDto {
  line1: string;
  city: string;
  state: string;
  zip: string;
}

export interface PropertyApiDto {
  id: string;
  ownerId: string;
  ownerName: string;
  ownerEmail: string;
  title: string;
  description: string;
  type: PropertyType;
  status: PropertyStatus;
  address: PropertyAddressApiDto;
  rentMonthly: number;
  bedrooms: number;
  bathrooms: number;
  sqft: number;
  amenities: string[];
  imageUrl: string;
  availableFrom: string;
  createdAt: string;
  updatedAt: string;
}

export interface UpsertPropertyApiRequest {
  title: string;
  description: string;
  type: PropertyType;
  status: PropertyStatus;
  address: PropertyAddressApiDto;
  rentMonthly: number;
  bedrooms: number;
  bathrooms: number;
  sqft: number;
  amenities: string[];
  imageUrl: string;
  availableFrom: string;
}

export function mapPropertyFromApi(dto: PropertyApiDto): Property {
  return {
    id: dto.id,
    ownerId: dto.ownerId,
    ownerName: dto.ownerName,
    ownerEmail: dto.ownerEmail,
    title: dto.title,
    description: dto.description,
    type: dto.type,
    status: dto.status,
    address: {
      line1: dto.address.line1,
      city: dto.address.city,
      state: dto.address.state,
      zip: dto.address.zip
    },
    rentMonthly: dto.rentMonthly,
    bedrooms: dto.bedrooms,
    bathrooms: dto.bathrooms,
    sqft: dto.sqft,
    amenities: dto.amenities ?? [],
    imageUrl: dto.imageUrl,
    availableFrom: dto.availableFrom,
    createdAt: dto.createdAt,
    updatedAt: dto.updatedAt
  };
}
